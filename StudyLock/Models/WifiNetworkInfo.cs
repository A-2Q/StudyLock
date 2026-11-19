namespace StudyLock.Models;

public sealed class WifiNetworkInfo
{
    public string Ssid { get; set; } = string.Empty;
    public int SignalQuality { get; set; }
    public bool IsSecured { get; set; }
    public bool IsConnected { get; set; }
    public bool HasSavedProfile { get; set; }
    public string AuthAlgorithm { get; set; } = "WPA2PSK";
    public string CipherAlgorithm { get; set; } = "AES";

    public string SignalBars => SignalQuality switch
    {
        >= 75 => "▂▄▆█",
        >= 50 => "▂▄▆░",
        >= 25 => "▂▄░░",
        _ => "▂░░░"
    };

    public string SecurityBadge => IsSecured ? "🔒" : "🔓";
}
