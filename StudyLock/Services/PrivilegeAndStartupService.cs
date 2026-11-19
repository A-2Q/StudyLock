using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Microsoft.Win32;

namespace StudyLock.Services;

public sealed class PrivilegeAndStartupService
{
    private const string ScheduledTaskName = "StudyLock_StrictRecovery";
    private const string RegistryRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegistryValueName = "StudyLock_StrictRecovery";

    public static bool IsRunningAsAdministrator()
    {
        try
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    public static bool TryRelaunchAsAdministrator(string arguments)
    {
        try
        {
            string? exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                return false;
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = exePath,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = Path.GetDirectoryName(exePath) ?? Environment.CurrentDirectory
            };

            Process? proc = Process.Start(startInfo);
            return proc is not null;
        }
        catch
        {
            // User declined UAC or OS error
            return false;
        }
    }

    public static void RegisterStrictAutoStart()
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return;
        }

        if (IsRunningAsAdministrator())
        {
            // Register elevated Task Scheduler logon task (runs without UAC prompt after reboot)
            bool taskCreated = RunSchtasks(
                $"/Create /TN \"{ScheduledTaskName}\" /TR \"\\\"{exePath}\\\" --strict-resume\" /SC ONLOGON /RL HIGHEST /F");

            if (taskCreated)
            {
                RemoveRegistryRunEntry();
                return;
            }
        }

        // Fallback to HKCU Run key
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKeyPath, writable: true);
            runKey?.SetValue(RegistryValueName, $"\"{exePath}\" --strict-resume");
        }
        catch
        {
            // Ignore registry failure
        }
    }

    public static void UnregisterStrictAutoStart()
    {
        RunSchtasks($"/Delete /TN \"{ScheduledTaskName}\" /F");
        RemoveRegistryRunEntry();
    }

    private static void RemoveRegistryRunEntry()
    {
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RegistryRunKeyPath, writable: true);
            if (runKey?.GetValue(RegistryValueName) is not null)
            {
                runKey.DeleteValue(RegistryValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Ignore registry cleanup failure
        }
    }

    private static bool RunSchtasks(string arguments)
    {
        try
        {
            ProcessStartInfo psi = new()
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process? process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(4000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
