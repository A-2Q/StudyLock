using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class ChildLockWindow : Window
{
    private readonly SessionConfig _config;
    private readonly DomainGuardService _domainGuard;
    private readonly SystemGuardService _systemGuard = new();
    private readonly DispatcherTimer _tickTimer;
    private long _lastTickMs;
    private double _remainingSeconds;
    private bool _timeExpired;
    private bool _allowClose;

    public ChildLockWindow(SessionConfig config)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;

        _config = config;
        string normalizedUrl = DomainGuardService.NormalizeUrl(config.TargetUrl);
        _config.TargetUrl = normalizedUrl;

        _domainGuard = new DomainGuardService(normalizedUrl, SessionMode.Child);
        _remainingSeconds = config.GetClampedDurationMinutes() * 60;
        SiteNameText.Text = string.IsNullOrWhiteSpace(config.SiteTitle) ? _domainGuard.AllowedBaseDomain : config.SiteTitle;
        UpdateCountdownDisplay();

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _tickTimer.Tick += TickTimer_Tick;

        Loaded += ChildLockWindow_Loaded;
    }

    private async void ChildLockWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _systemGuard.Engage(this);
        _lastTickMs = Environment.TickCount64;
        _tickTimer.Start();

        try
        {
            CoreWebView2Environment env = await App.WebViewEnvironments.GetEnvironmentAsync(SessionMode.Child);
            await ChildWebView.EnsureCoreWebView2Async(env);

            ChildWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            ChildWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            ChildWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            ChildWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

            ChildWebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            ChildWebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            ChildWebView.CoreWebView2.Navigate(_config.TargetUrl);
        }
        catch
        {
            // Keep screen locked safely even if WebView fails
        }
    }

    private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!_domainGuard.IsNavigationAllowed(e.Uri))
        {
            e.Cancel = true;
        }
    }

    private void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (_domainGuard.IsNavigationAllowed(e.Uri))
        {
            ChildWebView.CoreWebView2?.Navigate(e.Uri);
        }
    }

    private void TickTimer_Tick(object? sender, EventArgs e)
    {
        if (_timeExpired)
        {
            return;
        }

        long nowMs = Environment.TickCount64;
        double deltaSec = Math.Max(0, (nowMs - _lastTickMs) / 1000.0);
        _lastTickMs = nowMs;

        _remainingSeconds = Math.Max(0, _remainingSeconds - deltaSec);
        UpdateCountdownDisplay();

        if (_remainingSeconds <= 0)
        {
            EnterTimeUpState();
        }
    }

    private void UpdateCountdownDisplay()
    {
        TimeSpan ts = TimeSpan.FromSeconds(Math.Ceiling(_remainingSeconds));
        CountdownText.Text = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private void EnterTimeUpState()
    {
        _timeExpired = true;
        _tickTimer.Stop();

        try
        {
            // Stop any playing video/audio cleanly
            ChildWebView.CoreWebView2?.Navigate("about:blank");
        }
        catch
        {
            // Ignore
        }

        ChildWebView.Visibility = Visibility.Collapsed;
        TimeUpOverlay.Visibility = Visibility.Visible;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_timeExpired && ChildWebView.CoreWebView2?.CanGoBack == true)
        {
            ChildWebView.CoreWebView2.GoBack();
        }
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_timeExpired && ChildWebView.CoreWebView2 is not null)
        {
            ChildWebView.CoreWebView2.Navigate(_config.TargetUrl);
        }
    }

    private void ParentExitButton_Click(object sender, RoutedEventArgs e)
    {
        AppSettings settings = App.StateService.LoadSettings();
        ParentGateDialog gate = new(settings) { Owner = this };
        if (gate.ShowDialog() == true && gate.IsUnlocked)
        {
            UnlockAndReturnToDashboard();
        }
    }

    private void UnlockAndReturnToDashboard()
    {
        _tickTimer.Stop();
        _systemGuard.Dispose();
        _allowClose = true;

        MainWindow dashboard = new();
        Application.Current.MainWindow = dashboard;
        dashboard.Show();
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        _tickTimer.Stop();
        _systemGuard.Dispose();
        try
        {
            ChildWebView.Dispose();
        }
        catch
        {
            // Ignore disposal race
        }
    }
}
