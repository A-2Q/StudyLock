using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace StudyLock.Services;

public sealed class WindowsHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyDown = 0x0104;
    private const int WmSysKeyUp = 0x0105;

    private const int VkTab = 0x09;
    private const int VkEscape = 0x1B;
    private const int VkLWin = 0x5B;
    private const int VkRWin = 0x5C;
    private const int VkF4 = 0x73;
    private const int VkControl = 0x11;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int VkShift = 0x10;
    private const int VkLShift = 0xA0;
    private const int VkRShift = 0xA1;
    private const int VkD = 0x44;
    private const int VkE = 0x45;
    private const int VkR = 0x52;

    private const uint LlkhfAltDown = 0x20;

    private static readonly string[] BlockedProcessNames =
    [
        "Taskmgr",
        "procexp",
        "procexp64",
        "ProcessHacker",
        "SystemInformer"
    ];

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KbdLlHookStruct
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    private readonly LowLevelKeyboardProc _hookCallback;
    private IntPtr _hookHandle = IntPtr.Zero;
    private DispatcherTimer? _watchdogTimer;
    private Window? _hostWindow;
    private readonly int _currentProcessId = Environment.ProcessId;
    private bool _isActive;
    private bool _disposed;

    public WindowsHookService()
    {
        _hookCallback = HookCallback;
    }

    public bool IsActive => _isActive;

    public void Start(Window hostWindow)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isActive)
        {
            return;
        }

        _hostWindow = hostWindow;
        InstallKeyboardHook();
        StartWatchdog();
        _isActive = true;
    }

    public void Stop()
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;

        if (_watchdogTimer is not null)
        {
            _watchdogTimer.Stop();
            _watchdogTimer.Tick -= WatchdogTimer_Tick;
            _watchdogTimer = null;
        }

        UninstallKeyboardHook();
        _hostWindow = null;
    }

    private void InstallKeyboardHook()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        using Process currentProcess = Process.GetCurrentProcess();
        using ProcessModule? currentModule = currentProcess.MainModule;
        IntPtr moduleHandle = currentModule is not null
            ? GetModuleHandle(currentModule.ModuleName)
            : IntPtr.Zero;

        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _hookCallback, moduleHandle, 0);
    }

    private void UninstallKeyboardHook()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private void StartWatchdog()
    {
        _watchdogTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _watchdogTimer.Tick += WatchdogTimer_Tick;
        _watchdogTimer.Start();
    }

    private void WatchdogTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isActive || _hostWindow is null)
        {
            return;
        }

        TerminateBlockedProcesses();
        EnforceForegroundFocus();
    }

    private static void TerminateBlockedProcesses()
    {
        foreach (string procName in BlockedProcessNames)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(procName);
                foreach (Process proc in processes)
                {
                    using (proc)
                    {
                        try
                        {
                            proc.Kill(entireProcessTree: true);
                        }
                        catch
                        {
                            // Access denied when running standard user against elevated Taskmgr
                        }
                    }
                }
            }
            catch
            {
                // Ignore enumeration errors
            }
        }
    }

    private void EnforceForegroundFocus()
    {
        if (_hostWindow is null || !_hostWindow.IsVisible)
        {
            return;
        }

        IntPtr foregroundHwnd = GetForegroundWindow();
        if (foregroundHwnd != IntPtr.Zero)
        {
            _ = GetWindowThreadProcessId(foregroundHwnd, out uint foregroundPid);
            if (foregroundPid == (uint)_currentProcessId)
            {
                return;
            }
        }

        try
        {
            if (_hostWindow.WindowState == WindowState.Minimized)
            {
                _hostWindow.WindowState = WindowState.Maximized;
            }

            _hostWindow.Topmost = false;
            _hostWindow.Topmost = true;
            _hostWindow.Activate();

            IntPtr hostHwnd = new WindowInteropHelper(_hostWindow).Handle;
            if (hostHwnd != IntPtr.Zero)
            {
                SetForegroundWindow(hostHwnd);
            }
        }
        catch
        {
            // Ignore transient window activation race
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _isActive)
        {
            int msg = wParam.ToInt32();
            if (msg is WmKeyDown or WmSysKeyDown or WmKeyUp or WmSysKeyUp)
            {
                KbdLlHookStruct kb = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
                bool altDown = (kb.flags & LlkhfAltDown) != 0;
                bool ctrlDown = IsKeyDown(VkControl) || IsKeyDown(VkLControl) || IsKeyDown(VkRControl);
                bool shiftDown = IsKeyDown(VkShift) || IsKeyDown(VkLShift) || IsKeyDown(VkRShift);
                bool winDown = IsKeyDown(VkLWin) || IsKeyDown(VkRWin);

                // Block Windows keys (prevents Start Menu, Win+Tab, Win+D, Win+R, Win+E, etc.)
                if (kb.vkCode == VkLWin || kb.vkCode == VkRWin)
                {
                    return (IntPtr)1;
                }

                // Block Win + D / E / R / Tab combinations explicitly
                if (winDown && (kb.vkCode == VkD || kb.vkCode == VkE || kb.vkCode == VkR || kb.vkCode == VkTab))
                {
                    return (IntPtr)1;
                }

                // Block Alt+Tab, Alt+F4, Alt+Esc
                if (altDown && (kb.vkCode == VkTab || kb.vkCode == VkF4 || kb.vkCode == VkEscape))
                {
                    return (IntPtr)1;
                }

                // Block Ctrl+Esc (Start menu) and Ctrl+Shift+Esc (Task Manager)
                if (ctrlDown && kb.vkCode == VkEscape)
                {
                    return (IntPtr)1;
                }

                if (ctrlDown && shiftDown && kb.vkCode == VkEscape)
                {
                    return (IntPtr)1;
                }
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsKeyDown(int vkCode) => (GetAsyncKeyState(vkCode) & 0x8000) != 0;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
