using System.Windows;

namespace StudyLock.Services;

public sealed class SystemGuardService : IDisposable
{
    private readonly WindowsHookService _hookService = new();
    private readonly MultiMonitorService _multiMonitorService = new();
    private bool _isEngaged;
    private bool _disposed;

    public bool IsEngaged => _isEngaged;

    public void Engage(Window primaryLockWindow)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isEngaged)
        {
            return;
        }

        MultiMonitorService.PositionWindowOnPrimaryMonitor(primaryLockWindow);
        _multiMonitorService.Start();
        _hookService.Start(primaryLockWindow);
        _isEngaged = true;
    }

    public void Disengage()
    {
        if (!_isEngaged)
        {
            return;
        }

        _isEngaged = false;
        _hookService.Stop();
        _multiMonitorService.Stop();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disengage();
        _hookService.Dispose();
        _multiMonitorService.Dispose();
        _disposed = true;
    }
}
