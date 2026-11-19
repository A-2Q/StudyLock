using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;
using StudyLock.Views;

namespace StudyLock.Services;

public sealed class MultiMonitorService : IDisposable
{
    private const uint MonitorInfoPrimaryFlag = 1;
    private static readonly IntPtr HwndTopMost = new(-1);
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpShowWindow = 0x0040;

    private readonly List<BlackoutWindow> _blackoutWindows = [];
    private bool _isActive;
    private bool _disposed;

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfoEx
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isActive)
        {
            return;
        }

        _isActive = true;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        RefreshBlackoutWindows();
    }

    public void Stop()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        CloseAllBlackoutWindows();
    }

    public static void PositionWindowOnPrimaryMonitor(Window window)
    {
        List<MonitorInfoEx> monitors = EnumerateMonitors();
        MonitorInfoEx? primary = monitors.FirstOrDefault(m => (m.dwFlags & MonitorInfoPrimaryFlag) != 0);
        if (primary is null && monitors.Count > 0)
        {
            primary = monitors[0];
        }

        if (primary is null)
        {
            return;
        }

        Rect bounds = primary.Value.rcMonitor;
        IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
        SetWindowPos(
            hwnd,
            HwndTopMost,
            bounds.Left,
            bounds.Top,
            bounds.Right - bounds.Left,
            bounds.Bottom - bounds.Top,
            SwpShowWindow);
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        Application.Current?.Dispatcher.BeginInvoke(RefreshBlackoutWindows);
    }

    private void RefreshBlackoutWindows()
    {
        CloseAllBlackoutWindows();

        if (!_isActive)
        {
            return;
        }

        List<MonitorInfoEx> monitors = EnumerateMonitors();
        foreach (MonitorInfoEx monitor in monitors)
        {
            if ((monitor.dwFlags & MonitorInfoPrimaryFlag) != 0)
            {
                continue;
            }

            Rect rect = monitor.rcMonitor;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            BlackoutWindow blackout = new();
            blackout.Show();
            IntPtr hwnd = new WindowInteropHelper(blackout).Handle;
            if (hwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, HwndTopMost, rect.Left, rect.Top, width, height, SwpNoActivate | SwpShowWindow);
            }

            _blackoutWindows.Add(blackout);
        }
    }

    private void CloseAllBlackoutWindows()
    {
        foreach (BlackoutWindow window in _blackoutWindows)
        {
            try
            {
                window.AllowClose();
                window.Close();
            }
            catch
            {
                // Ignore already-closed window
            }
        }
        _blackoutWindows.Clear();
    }

    private static List<MonitorInfoEx> EnumerateMonitors()
    {
        List<MonitorInfoEx> monitors = [];
        EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (IntPtr hMonitor, IntPtr _, ref Rect _, IntPtr _) =>
            {
                MonitorInfoEx info = new()
                {
                    cbSize = Marshal.SizeOf<MonitorInfoEx>(),
                    szDevice = string.Empty
                };

                if (GetMonitorInfo(hMonitor, ref info))
                {
                    monitors.Add(info);
                }

                return true;
            },
            IntPtr.Zero);

        return monitors;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfoEx lpmi);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
}
