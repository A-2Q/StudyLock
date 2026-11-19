using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using StudyLock.Models;

namespace StudyLock.Services;

public sealed class WifiService
{
    private const uint WlanApiVersion = 2;
    private const uint WlanAvailableNetworkConnected = 0x00000001;
    private const uint WlanAvailableNetworkHasProfile = 0x00000002;
    private const uint WlanAvailableNetworkIncludeAllAdhocProfiles = 0x00000001;
    private const uint WlanAvailableNetworkIncludeAllManualHiddenProfiles = 0x00000002;

    public async Task<(bool Success, string? ErrorMessage, List<WifiNetworkInfo> Networks)> ScanAndGetNetworksAsync()
    {
        return await Task.Run<(bool Success, string? ErrorMessage, List<WifiNetworkInfo> Networks)>(() =>
        {
            List<WifiNetworkInfo> results = [];
            IntPtr clientHandle = IntPtr.Zero;
            IntPtr interfaceListPtr = IntPtr.Zero;

            try
            {
                uint openResult = WlanOpenHandle(WlanApiVersion, IntPtr.Zero, out _, out clientHandle);
                if (openResult != 0)
                {
                    return (false, "WLAN service is unavailable on this device.", results);
                }

                uint enumResult = WlanEnumInterfaces(clientHandle, IntPtr.Zero, out interfaceListPtr);
                if (enumResult != 0 || interfaceListPtr == IntPtr.Zero)
                {
                    return (false, "No Wi-Fi adapter detected.", results);
                }

                int interfaceCount = Marshal.ReadInt32(interfaceListPtr, 0);
                if (interfaceCount <= 0)
                {
                    return (false, "No active Wi-Fi adapter found.", results);
                }

                IntPtr firstInterfacePtr = IntPtr.Add(interfaceListPtr, 8);
                WlanInterfaceInfo iface = Marshal.PtrToStructure<WlanInterfaceInfo>(firstInterfacePtr);
                Guid interfaceGuid = iface.InterfaceGuid;

                // Trigger fresh scan (non-blocking)
                _ = WlanScan(clientHandle, ref interfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                uint listResult = WlanGetAvailableNetworkList(
                    clientHandle,
                    ref interfaceGuid,
                    WlanAvailableNetworkIncludeAllAdhocProfiles | WlanAvailableNetworkIncludeAllManualHiddenProfiles,
                    IntPtr.Zero,
                    out IntPtr networkListPtr);

                if (listResult != 0 || networkListPtr == IntPtr.Zero)
                {
                    return (false, "Unable to list available Wi-Fi networks.", results);
                }

                try
                {
                    int numItems = Marshal.ReadInt32(networkListPtr, 0);
                    int itemSize = Marshal.SizeOf<WlanAvailableNetwork>();
                    Dictionary<string, WifiNetworkInfo> dedup = new(StringComparer.Ordinal);

                    for (int i = 0; i < numItems; i++)
                    {
                        IntPtr itemPtr = IntPtr.Add(networkListPtr, 8 + i * itemSize);
                        WlanAvailableNetwork net = Marshal.PtrToStructure<WlanAvailableNetwork>(itemPtr);

                        string ssid = ExtractSsid(net.dot11Ssid);
                        if (string.IsNullOrWhiteSpace(ssid))
                        {
                            continue;
                        }

                        bool isConnected = (net.dwFlags & WlanAvailableNetworkConnected) != 0;
                        bool hasProfile = (net.dwFlags & WlanAvailableNetworkHasProfile) != 0;

                        WifiNetworkInfo info = new()
                        {
                            Ssid = ssid,
                            SignalQuality = (int)Math.Clamp(net.wlanSignalQuality, 0u, 100u),
                            IsSecured = net.bSecurityEnabled,
                            IsConnected = isConnected,
                            HasSavedProfile = hasProfile,
                            AuthAlgorithm = MapAuthAlgorithm(net.dot11DefaultAuthAlgorithm),
                            CipherAlgorithm = MapCipherAlgorithm(net.dot11DefaultCipherAlgorithm)
                        };

                        if (dedup.TryGetValue(ssid, out WifiNetworkInfo? existing))
                        {
                            if (info.IsConnected || (!existing.IsConnected && info.SignalQuality > existing.SignalQuality))
                            {
                                info.HasSavedProfile = info.HasSavedProfile || existing.HasSavedProfile;
                                dedup[ssid] = info;
                            }
                        }
                        else
                        {
                            dedup[ssid] = info;
                        }
                    }

                    results = dedup.Values
                        .OrderByDescending(n => n.IsConnected)
                        .ThenByDescending(n => n.SignalQuality)
                        .ThenBy(n => n.Ssid, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return (true, null, results);
                }
                finally
                {
                    WlanFreeMemory(networkListPtr);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message, results);
            }
            finally
            {
                if (interfaceListPtr != IntPtr.Zero)
                {
                    WlanFreeMemory(interfaceListPtr);
                }
                if (clientHandle != IntPtr.Zero)
                {
                    WlanCloseHandle(clientHandle, IntPtr.Zero);
                }
            }
        });
    }

    public async Task<(bool Success, string? ErrorMessage)> ConnectAsync(WifiNetworkInfo network, string? password = null)
    {
        return await Task.Run<(bool Success, string? ErrorMessage)>(() =>
        {
            IntPtr clientHandle = IntPtr.Zero;
            IntPtr interfaceListPtr = IntPtr.Zero;

            try
            {
                uint openResult = WlanOpenHandle(WlanApiVersion, IntPtr.Zero, out _, out clientHandle);
                if (openResult != 0)
                {
                    return (false, "WLAN service is unavailable.");
                }

                uint enumResult = WlanEnumInterfaces(clientHandle, IntPtr.Zero, out interfaceListPtr);
                if (enumResult != 0 || interfaceListPtr == IntPtr.Zero)
                {
                    return (false, "No Wi-Fi adapter detected.");
                }

                int interfaceCount = Marshal.ReadInt32(interfaceListPtr, 0);
                if (interfaceCount <= 0)
                {
                    return (false, "No active Wi-Fi adapter found.");
                }

                IntPtr firstInterfacePtr = IntPtr.Add(interfaceListPtr, 8);
                WlanInterfaceInfo iface = Marshal.PtrToStructure<WlanInterfaceInfo>(firstInterfacePtr);
                Guid interfaceGuid = iface.InterfaceGuid;

                // Create/update profile if password provided or open network without saved profile
                if (!network.HasSavedProfile || !string.IsNullOrEmpty(password))
                {
                    string profileXml = BuildProfileXml(network, password ?? string.Empty);
                    uint setResult = WlanSetProfile(
                        clientHandle,
                        ref interfaceGuid,
                        0,
                        profileXml,
                        null,
                        true,
                        IntPtr.Zero,
                        out uint reasonCode);

                    if (setResult != 0)
                    {
                        return (false, $"Failed to create Wi-Fi profile (code {setResult}, reason {reasonCode}).");
                    }
                }

                WlanConnectionParameters connParams = new()
                {
                    wlanConnectionMode = WlanConnectionMode.Profile,
                    strProfile = network.Ssid,
                    pDot11Ssid = IntPtr.Zero,
                    pDesiredBssidList = IntPtr.Zero,
                    dot11BssType = Dot11BssType.Infrastructure,
                    dwFlags = 0
                };

                uint connectResult = WlanConnect(clientHandle, ref interfaceGuid, ref connParams, IntPtr.Zero);
                if (connectResult != 0)
                {
                    return (false, $"Failed to connect to '{network.Ssid}' (code {connectResult}).");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            finally
            {
                if (interfaceListPtr != IntPtr.Zero)
                {
                    WlanFreeMemory(interfaceListPtr);
                }
                if (clientHandle != IntPtr.Zero)
                {
                    WlanCloseHandle(clientHandle, IntPtr.Zero);
                }
            }
        });
    }

    private static string BuildProfileXml(WifiNetworkInfo network, string password)
    {
        string escapedSsid = SecurityElement.Escape(network.Ssid) ?? network.Ssid;
        string hexSsid = Convert.ToHexString(Encoding.UTF8.GetBytes(network.Ssid));

        if (!network.IsSecured || string.IsNullOrEmpty(password))
        {
            return $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{escapedSsid}</name>
  <SSIDConfig>
    <SSID>
      <hex>{hexSsid}</hex>
      <name>{escapedSsid}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>open</authentication>
        <encryption>none</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
    </security>
  </MSM>
</WLANProfile>";
        }

        string escapedPass = SecurityElement.Escape(password) ?? password;
        string auth = network.AuthAlgorithm is "WPA3SAE" or "WPA2PSK" or "WPAPSK"
            ? network.AuthAlgorithm
            : "WPA2PSK";
        string cipher = network.CipherAlgorithm is "AES" or "TKIP"
            ? network.CipherAlgorithm
            : "AES";

        return $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
  <name>{escapedSsid}</name>
  <SSIDConfig>
    <SSID>
      <hex>{hexSsid}</hex>
      <name>{escapedSsid}</name>
    </SSID>
  </SSIDConfig>
  <connectionType>ESS</connectionType>
  <connectionMode>auto</connectionMode>
  <MSM>
    <security>
      <authEncryption>
        <authentication>{auth}</authentication>
        <encryption>{cipher}</encryption>
        <useOneX>false</useOneX>
      </authEncryption>
      <sharedKey>
        <keyType>passPhrase</keyType>
        <protected>false</protected>
        <keyMaterial>{escapedPass}</keyMaterial>
      </sharedKey>
    </security>
  </MSM>
</WLANProfile>";
    }

    private static string ExtractSsid(Dot11Ssid ssid)
    {
        if (ssid.uSSIDLength == 0 || ssid.ucSSID is null)
        {
            return string.Empty;
        }

        int len = (int)Math.Min(ssid.uSSIDLength, (uint)ssid.ucSSID.Length);
        return Encoding.UTF8.GetString(ssid.ucSSID, 0, len).TrimEnd('\0');
    }

    private static string MapAuthAlgorithm(uint auth) => auth switch
    {
        1 => "open",
        4 => "WPAPSK",
        7 => "WPA2PSK",
        9 => "WPA3SAE",
        _ => "WPA2PSK"
    };

    private static string MapCipherAlgorithm(uint cipher) => cipher switch
    {
        0 => "none",
        2 => "TKIP",
        4 => "AES",
        _ => "AES"
    };

    private enum WlanConnectionMode
    {
        Profile = 0
    }

    private enum Dot11BssType
    {
        Infrastructure = 1,
        Independent = 2,
        Any = 3
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanInterfaceInfo
    {
        public Guid InterfaceGuid;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strInterfaceDescription;
        public int isState;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Dot11Ssid
    {
        public uint uSSIDLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ucSSID;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanAvailableNetwork
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string strProfileName;
        public Dot11Ssid dot11Ssid;
        public Dot11BssType dot11BssType;
        public uint uNumberOfBssids;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bNetworkConnectable;
        public uint wlanNotConnectableReason;
        public uint uNumberOfPhyTypes;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public uint[] dot11PhyTypes;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bMorePhyTypes;
        public uint wlanSignalQuality;
        [MarshalAs(UnmanagedType.Bool)]
        public bool bSecurityEnabled;
        public uint dot11DefaultAuthAlgorithm;
        public uint dot11DefaultCipherAlgorithm;
        public uint dwFlags;
        public uint dwReserved;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WlanConnectionParameters
    {
        public WlanConnectionMode wlanConnectionMode;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string strProfile;
        public IntPtr pDot11Ssid;
        public IntPtr pDesiredBssidList;
        public Dot11BssType dot11BssType;
        public uint dwFlags;
    }

    [DllImport("wlanapi.dll")]
    private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanScan(IntPtr hClientHandle, ref Guid pInterfaceGuid, IntPtr pDot11Ssid, IntPtr pIeData, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanGetAvailableNetworkList(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint dwFlags, IntPtr pReserved, out IntPtr ppAvailableNetworkList);

    [DllImport("wlanapi.dll", CharSet = CharSet.Unicode)]
    private static extern uint WlanSetProfile(IntPtr hClientHandle, ref Guid pInterfaceGuid, uint dwFlags, string strProfileXml, string? strAllUserProfileSecurity, [MarshalAs(UnmanagedType.Bool)] bool bOverwrite, IntPtr pReserved, out uint pdwReasonCode);

    [DllImport("wlanapi.dll")]
    private static extern uint WlanConnect(IntPtr hClientHandle, ref Guid pInterfaceGuid, ref WlanConnectionParameters pConnectionParameters, IntPtr pReserved);

    [DllImport("wlanapi.dll")]
    private static extern void WlanFreeMemory(IntPtr pMemory);
}
