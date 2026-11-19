using System.ComponentModel;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class StudyLockWindow : Window
{
    private const int GracePeriodDurationSeconds = 30;
    private const int AntiAfkIntervalSeconds = 15 * 60; // 15 minutes
    private const int AntiAfkGraceSeconds = 60;         // 60 seconds window

    private readonly SessionConfig _config;
    private readonly DomainGuardService _domainGuard;
    private readonly SystemGuardService _systemGuard = new();
    private readonly List<BrowserTabItem> _tabs = [];
    private readonly List<double> _antiAfkResponseTimes = [];
    private readonly DispatcherTimer _mainTimer;
    private readonly DispatcherTimer _toastTimer;
    private readonly DateTime _sessionStartUtc;
    private readonly string _sessionId;
    private readonly bool _antiAfkEnabled;

    private CoreWebView2Environment? _webViewEnv;
    private BrowserTabItem? _activeTab;
    private BrowserTabItem? _rightSplitTab;
    private bool _isSplitView;
    private bool _suppressSelectorEvents;

    private long _lastTickMs;
    private double _remainingSeconds;
    private double _elapsedStudySeconds;
    private double _graceSecondsRemaining;
    private double _antiAfkElapsedSeconds;
    private double _antiAfkPromptRemainingSeconds;
    private double _strictSaveAccumulator;

    private bool _isUserPaused;
    private bool _isAntiAfkPromptVisible;
    private bool _isAntiAfkFrozen;
    private bool _strictArmed;
    private bool _sessionFinished;
    private bool _allowClose;

    private string _notesContent = string.Empty;
    private QuickNotesWindow? _notesWindow;
    private CalculatorWindow? _calculatorWindow;
    private WifiFlyoutWindow? _wifiWindow;

    public StudyLockWindow(SessionConfig config, StrictSessionState? recoveredState = null)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;

        _config = recoveredState?.Config ?? config;
        _config.TargetUrl = DomainGuardService.NormalizeUrl(_config.TargetUrl);

        AppSettings settings = App.StateService.LoadSettings();
        _domainGuard = new DomainGuardService(
            _config.TargetUrl,
            SessionMode.Study,
            _config.AttachedPdfPaths,
            settings.OAuthWhitelistHosts);

        int plannedMinutes = _config.GetClampedDurationMinutes();
        _antiAfkEnabled = plannedMinutes > 15;

        if (recoveredState is not null)
        {
            _sessionId = recoveredState.SessionId;
            _sessionStartUtc = recoveredState.StartTimeUtc;
            _remainingSeconds = Math.Max(1, recoveredState.RemainingSeconds);
            _elapsedStudySeconds = recoveredState.ElapsedStudySeconds;
            _antiAfkElapsedSeconds = recoveredState.AntiAfkElapsedSeconds;
            _antiAfkResponseTimes.AddRange(recoveredState.AntiAfkResponseTimes);
            _notesContent = recoveredState.Notes ?? string.Empty;
            _graceSecondsRemaining = 0;
            _strictArmed = true;
        }
        else
        {
            _sessionId = Guid.NewGuid().ToString("N");
            _sessionStartUtc = DateTime.UtcNow;
            _remainingSeconds = plannedMinutes * 60;
            _elapsedStudySeconds = 0;
            _antiAfkElapsedSeconds = 0;
            _graceSecondsRemaining = _config.SkipGracePeriod ? 0 : GracePeriodDurationSeconds;
            _strictArmed = _config.LockLevel == LockLevel.Strict && _graceSecondsRemaining <= 0;
        }

        LockModeIcon.Text = _config.LockLevel == LockLevel.Strict ? "🛡️" : "🔒";
        NextCheckContainer.Visibility = _antiAfkEnabled ? Visibility.Visible : Visibility.Collapsed;

        if (_graceSecondsRemaining > 0)
        {
            GraceBanner.Visibility = Visibility.Visible;
            GraceCountdownText.Text = $"{(int)Math.Ceiling(_graceSecondsRemaining)}s";
        }

        UpdateTimerDisplays();

        _mainTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _mainTimer.Tick += MainTimer_Tick;

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            BlockedToastBanner.Visibility = Visibility.Collapsed;
        };

        Loaded += StudyLockWindow_Loaded;
    }

    private async void StudyLockWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _systemGuard.Engage(this);

        if (_strictArmed)
        {
            ArmStrictPersistence();
        }

        _lastTickMs = Environment.TickCount64;
        _mainTimer.Start();

        try
        {
            _webViewEnv = await App.WebViewEnvironments.GetEnvironmentAsync(SessionMode.Study);

            // 1. Create primary study site tab
            string primaryTitle = string.IsNullOrWhiteSpace(_config.SiteTitle)
                ? _domainGuard.AllowedBaseDomain
                : _config.SiteTitle;

            BrowserTabItem primaryTab = await CreateTabAsync(_config.TargetUrl, primaryTitle, BrowserTabKind.Web, isPrimary: true);

            // 2. Create a tab for each attached local PDF
            foreach (string pdfPath in _config.AttachedPdfPaths)
            {
                if (!string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath))
                {
                    string fileUri = new Uri(Path.GetFullPath(pdfPath)).AbsoluteUri;
                    string pdfName = Path.GetFileName(pdfPath);
                    await CreateTabAsync(fileUri, pdfName, BrowserTabKind.Pdf, isPrimary: false);
                }
            }

            SelectTab(primaryTab);
            RefreshSplitSelectors();
        }
        catch (Exception ex)
        {
            ShowBlockedToast($"WebView2 Error: {ex.Message}");
        }
    }

    // -------------------------------------------------------------------------
    // Tab & WebView2 Management + Split View
    // -------------------------------------------------------------------------
    private async Task<BrowserTabItem> CreateTabAsync(string url, string initialTitle, BrowserTabKind kind, bool isPrimary)
    {
        WebView2 wv = new();
        BrowserTabItem tab = new()
        {
            Title = initialTitle,
            CurrentUrl = url,
            Kind = kind,
            IsPrimary = isPrimary,
            WebView = wv
        };

        _tabs.Add(tab);
        RebuildTabStrip();

        if (_webViewEnv is not null)
        {
            await wv.EnsureCoreWebView2Async(_webViewEnv);
            wv.CoreWebView2.Settings.AreDevToolsEnabled = false;
            wv.CoreWebView2.Settings.IsStatusBarEnabled = false;

            wv.CoreWebView2.NavigationStarting += (_, e) =>
            {
                if (!_domainGuard.IsNavigationAllowed(e.Uri))
                {
                    e.Cancel = true;
                    ShowBlockedToast(LocalizationAndThemeService.GetString("BlockedDomainToast", "Blocked external domain."));
                }
            };

            wv.CoreWebView2.NewWindowRequested += async (_, e) =>
            {
                e.Handled = true;
                if (_domainGuard.IsNavigationAllowed(e.Uri))
                {
                    BrowserTabItem newTab = await CreateTabAsync(e.Uri, _domainGuard.AllowedBaseDomain, BrowserTabKind.Web, isPrimary: false);
                    SelectTab(newTab);
                    RefreshSplitSelectors();
                }
                else
                {
                    ShowBlockedToast(LocalizationAndThemeService.GetString("BlockedDomainToast", "Blocked external popup."));
                }
            };

            wv.CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                if (tab.Kind == BrowserTabKind.Web && !string.IsNullOrWhiteSpace(wv.CoreWebView2.DocumentTitle))
                {
                    tab.Title = TruncateTitle(wv.CoreWebView2.DocumentTitle);
                    RebuildTabStrip();
                    RefreshSplitSelectors();
                }
            };

            wv.CoreWebView2.SourceChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(wv.CoreWebView2.Source))
                {
                    tab.CurrentUrl = wv.CoreWebView2.Source;
                }
            };

            wv.CoreWebView2.Navigate(url);
        }

        return tab;
    }

    private void SelectTab(BrowserTabItem tab)
    {
        _activeTab = tab;
        foreach (BrowserTabItem t in _tabs)
        {
            t.IsActive = ReferenceEquals(t, tab);
        }

        MountViewports();
        RebuildTabStrip();
    }

    private void CloseTab(BrowserTabItem tab)
    {
        if (!tab.CanClose)
        {
            return;
        }

        _tabs.Remove(tab);
        DetachWebView(tab.WebView);
        try
        {
            tab.WebView?.Dispose();
        }
        catch
        {
            // Ignore disposal race
        }

        if (ReferenceEquals(_activeTab, tab))
        {
            _activeTab = _tabs.FirstOrDefault();
        }

        if (ReferenceEquals(_rightSplitTab, tab))
        {
            _rightSplitTab = _tabs.FirstOrDefault(t => !ReferenceEquals(t, _activeTab)) ?? _tabs.FirstOrDefault();
        }

        MountViewports();
        RebuildTabStrip();
        RefreshSplitSelectors();
    }

    private void MountViewports()
    {
        LeftWebHost.Children.Clear();
        RightWebHost.Children.Clear();

        if (!_isSplitView)
        {
            LeftColumnDef.Width = new GridLength(1, GridUnitType.Star);
            SplitterColumnDef.Width = new GridLength(0);
            RightColumnDef.Width = new GridLength(0);
            SplitDivider.Visibility = Visibility.Collapsed;
            RightWebHost.Visibility = Visibility.Collapsed;

            if (_activeTab?.WebView is not null)
            {
                DetachWebView(_activeTab.WebView);
                LeftWebHost.Children.Add(_activeTab.WebView);
            }
        }
        else
        {
            LeftColumnDef.Width = new GridLength(1, GridUnitType.Star);
            SplitterColumnDef.Width = new GridLength(6);
            RightColumnDef.Width = new GridLength(1, GridUnitType.Star);
            SplitDivider.Visibility = Visibility.Visible;
            RightWebHost.Visibility = Visibility.Visible;

            if (_activeTab?.WebView is not null)
            {
                DetachWebView(_activeTab.WebView);
                LeftWebHost.Children.Add(_activeTab.WebView);
            }

            if (_rightSplitTab is null || ReferenceEquals(_rightSplitTab, _activeTab))
            {
                _rightSplitTab = _tabs.FirstOrDefault(t => !ReferenceEquals(t, _activeTab));
            }

            if (_rightSplitTab?.WebView is not null && !ReferenceEquals(_rightSplitTab, _activeTab))
            {
                DetachWebView(_rightSplitTab.WebView);
                RightWebHost.Children.Add(_rightSplitTab.WebView);
            }
            else
            {
                // Informative placeholder if only 1 tab exists in split mode
                RightWebHost.Children.Add(new TextBlock
                {
                    Text = App.Localization.CurrentLanguage == "ar"
                        ? "افتح تبويباً آخر أو أرفق ملف PDF لعرضه هنا جنباً إلى جنب."
                        : "Open another tab or attach a PDF to view it side by side here.",
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
        }
    }

    private static void DetachWebView(WebView2? wv)
    {
        if (wv?.Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(wv);
        }
    }

    private void RebuildTabStrip()
    {
        TabsPanel.Children.Clear();
        foreach (BrowserTabItem tab in _tabs)
        {
            bool isSelected = ReferenceEquals(tab, _activeTab);
            Border pill = new()
            {
                Background = (Brush)FindResource(isSelected ? "AccentSubtleBrush" : "BgSurface1Brush"),
                BorderBrush = (Brush)FindResource(isSelected ? "BorderAccentBrush" : "BorderSubtleBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            StackPanel content = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            content.Children.Add(new TextBlock
            {
                Text = tab.IconGlyph,
                FontSize = 12,
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            content.Children.Add(new TextBlock
            {
                Text = tab.Title,
                FontSize = 12.5,
                FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = (Brush)FindResource(isSelected ? "AccentPrimaryBrush" : "TextPrimaryBrush"),
                MaxWidth = 170,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            });

            if (tab.CanClose)
            {
                Button closeBtn = new()
                {
                    Content = "✕",
                    Style = (Style)FindResource("GhostButtonStyle"),
                    Padding = new Thickness(5, 1, 5, 1),
                    Margin = new Thickness(6, 0, 0, 0),
                    FontSize = 10
                };
                closeBtn.Click += (s, args) =>
                {
                    args.Handled = true;
                    CloseTab(tab);
                };
                content.Children.Add(closeBtn);
            }

            pill.Child = content;
            pill.MouseLeftButtonUp += (_, _) => SelectTab(tab);
            TabsPanel.Children.Add(pill);
        }
    }

    private void RefreshSplitSelectors()
    {
        _suppressSelectorEvents = true;
        try
        {
            LeftPaneSelector.ItemsSource = null;
            RightPaneSelector.ItemsSource = null;
            LeftPaneSelector.ItemsSource = _tabs;
            RightPaneSelector.ItemsSource = _tabs;
            LeftPaneSelector.DisplayMemberPath = nameof(BrowserTabItem.Title);
            RightPaneSelector.DisplayMemberPath = nameof(BrowserTabItem.Title);
            LeftPaneSelector.SelectedItem = _activeTab;
            RightPaneSelector.SelectedItem = _rightSplitTab;
        }
        finally
        {
            _suppressSelectorEvents = false;
        }
    }

    private void PaneSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectorEvents || !_isSplitView)
        {
            return;
        }

        if (LeftPaneSelector.SelectedItem is BrowserTabItem leftTab)
        {
            _activeTab = leftTab;
        }

        if (RightPaneSelector.SelectedItem is BrowserTabItem rightTab)
        {
            _rightSplitTab = rightTab;
        }

        MountViewports();
        RebuildTabStrip();
    }

    private void SplitToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isSplitView = !_isSplitView;
        SplitSelectorsPanel.Visibility = _isSplitView ? Visibility.Visible : Visibility.Collapsed;
        SplitToggleButton.SetResourceReference(
            ContentProperty,
            _isSplitView ? "SingleViewBtn" : "SplitViewBtn");

        if (_isSplitView && (_rightSplitTab is null || ReferenceEquals(_rightSplitTab, _activeTab)))
        {
            _rightSplitTab = _tabs.FirstOrDefault(t => !ReferenceEquals(t, _activeTab));
        }

        MountViewports();
        RefreshSplitSelectors();
    }

    private void NavBack_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab?.WebView?.CoreWebView2?.CanGoBack == true)
        {
            _activeTab.WebView.CoreWebView2.GoBack();
        }
    }

    private void NavForward_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab?.WebView?.CoreWebView2?.CanGoForward == true)
        {
            _activeTab.WebView.CoreWebView2.GoForward();
        }
    }

    private void NavReload_Click(object sender, RoutedEventArgs e)
    {
        _activeTab?.WebView?.CoreWebView2?.Reload();
    }

    // -------------------------------------------------------------------------
    // Timers, Grace Period, Anti-AFK (15-Min + Fullscreen Video Exit), Strict Save
    // -------------------------------------------------------------------------
    private async void MainTimer_Tick(object? sender, EventArgs e)
    {
        if (_sessionFinished)
        {
            return;
        }

        long nowMs = Environment.TickCount64;
        double deltaSec = Math.Max(0, (nowMs - _lastTickMs) / 1000.0);
        _lastTickMs = nowMs;

        // 1. Handle 30-second Grace Period
        if (_graceSecondsRemaining > 0)
        {
            _graceSecondsRemaining = Math.Max(0, _graceSecondsRemaining - deltaSec);
            GraceCountdownText.Text = $"{(int)Math.Ceiling(_graceSecondsRemaining)}s";

            if (_graceSecondsRemaining <= 0)
            {
                SealLockAfterGrace();
            }
        }

        // 2. Handle Active Anti-AFK 60s response window
        if (_isAntiAfkPromptVisible && !_isAntiAfkFrozen)
        {
            _antiAfkPromptRemainingSeconds = Math.Max(0, _antiAfkPromptRemainingSeconds - deltaSec);
            AntiAfkGraceText.Text = $"{(int)Math.Ceiling(_antiAfkPromptRemainingSeconds)}s";

            if (_antiAfkPromptRemainingSeconds <= 0)
            {
                // Freeze main timer fairly with ZERO penalty
                _isAntiAfkFrozen = true;
                AntiAfkMessageText.SetResourceReference(TextBlock.TextProperty, "AntiAfkBannerFrozen");
                AntiAfkGraceText.Text = "⏸";
            }
        }

        // 3. Advance Main Countdown ONLY when not user-paused and not AFK-frozen
        bool isTimerRunning = !_isUserPaused && !_isAntiAfkFrozen;
        if (isTimerRunning)
        {
            _remainingSeconds = Math.Max(0, _remainingSeconds - deltaSec);
            _elapsedStudySeconds += deltaSec;

            // Advance 15-minute Anti-AFK cycle if enabled & not already prompting
            if (_antiAfkEnabled && !_isAntiAfkPromptVisible && _remainingSeconds > 10)
            {
                _antiAfkElapsedSeconds += deltaSec;
                if (_antiAfkElapsedSeconds >= AntiAfkIntervalSeconds)
                {
                    await TriggerAntiAfkCheckAsync();
                }
            }
        }

        // 4. Periodic encrypted state write every 5 seconds in Strict Mode
        if (_strictArmed)
        {
            _strictSaveAccumulator += deltaSec;
            if (_strictSaveAccumulator >= 5.0)
            {
                _strictSaveAccumulator = 0;
                PersistStrictState();
            }
        }

        UpdateTimerDisplays();

        if (_remainingSeconds <= 0)
        {
            CompleteSessionNormally();
        }
    }

    private void SealLockAfterGrace()
    {
        _graceSecondsRemaining = 0;
        GraceBanner.Visibility = Visibility.Collapsed;

        if (_config.LockLevel == LockLevel.Strict && !_strictArmed)
        {
            _strictArmed = true;
            ArmStrictPersistence();
        }
    }

    private void ArmStrictPersistence()
    {
        PersistStrictState();
        PrivilegeAndStartupService.RegisterStrictAutoStart();
    }

    private void PersistStrictState()
    {
        StrictSessionState state = new()
        {
            SessionId = _sessionId,
            Config = _config,
            StartTimeUtc = _sessionStartUtc,
            RemainingSeconds = (int)Math.Ceiling(_remainingSeconds),
            ElapsedStudySeconds = (int)Math.Floor(_elapsedStudySeconds),
            AntiAfkElapsedSeconds = (int)Math.Floor(_antiAfkElapsedSeconds),
            AntiAfkResponseTimes = [.. _antiAfkResponseTimes],
            Notes = _notesContent,
            IsArmed = true
        };

        App.StateService.SaveStrictSession(state);
    }

    private async Task TriggerAntiAfkCheckAsync()
    {
        _isAntiAfkPromptVisible = true;
        _isAntiAfkFrozen = false;
        _antiAfkPromptRemainingSeconds = AntiAfkGraceSeconds;

        // Force exit HTML5 fullscreen video on any active WebView2 so banner is guaranteed visible
        foreach (BrowserTabItem tab in _tabs)
        {
            try
            {
                if (tab.WebView?.CoreWebView2 is { ContainsFullScreenElement: true } core)
                {
                    await core.ExecuteScriptAsync("if (document.fullscreenElement) { document.exitFullscreen().catch(()=>{}); }");
                }
            }
            catch
            {
                // Ignore script race
            }
        }

        AntiAfkMessageText.SetResourceReference(TextBlock.TextProperty, "AntiAfkBannerActive");
        AntiAfkGraceText.Text = $"{AntiAfkGraceSeconds}s";
        AntiAfkBanner.Visibility = Visibility.Visible;

        try
        {
            SystemSounds.Asterisk.Play();
        }
        catch
        {
            // Ignore audio device errors
        }
    }

    private void IAmStudyingButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isAntiAfkPromptVisible)
        {
            return;
        }

        double responseSeconds = _isAntiAfkFrozen
            ? AntiAfkGraceSeconds
            : Math.Clamp(AntiAfkGraceSeconds - _antiAfkPromptRemainingSeconds, 0.5, AntiAfkGraceSeconds);

        _antiAfkResponseTimes.Add(responseSeconds);
        _isAntiAfkPromptVisible = false;
        _isAntiAfkFrozen = false;
        _antiAfkElapsedSeconds = 0;
        AntiAfkBanner.Visibility = Visibility.Collapsed;
        _lastTickMs = Environment.TickCount64;
    }

    private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
    {
        _isUserPaused = !_isUserPaused;
        PausedStatusBadge.Visibility = _isUserPaused ? Visibility.Visible : Visibility.Collapsed;
        PauseResumeButton.SetResourceReference(
            ContentProperty,
            _isUserPaused ? "ResumeTimerBtn" : "PauseTimerBtn");
        _lastTickMs = Environment.TickCount64;
    }

    private void LockNowButton_Click(object sender, RoutedEventArgs e)
    {
        SealLockAfterGrace();
    }

    private void CancelStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_graceSecondsRemaining <= 0)
        {
            return;
        }

        // Clean abort during 30s grace window without saving strict state or history penalty
        CleanupAndReturnToDashboard();
    }

    private void UpdateTimerDisplays()
    {
        TimeSpan remainingTs = TimeSpan.FromSeconds(Math.Ceiling(_remainingSeconds));
        MainCountdownText.Text = $"{(int)remainingTs.TotalHours:D2}:{remainingTs.Minutes:D2}:{remainingTs.Seconds:D2}";

        if (_antiAfkEnabled)
        {
            double secsToNext = Math.Max(0, AntiAfkIntervalSeconds - _antiAfkElapsedSeconds);
            TimeSpan nextTs = TimeSpan.FromSeconds(Math.Ceiling(secsToNext));
            NextCheckCountdownText.Text = $"{nextTs.Minutes:D2}:{nextTs.Seconds:D2}";
        }
    }

    // -------------------------------------------------------------------------
    // Floating Airspace-Safe Tool Windows
    // -------------------------------------------------------------------------
    private void NotesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_notesWindow is null)
        {
            _notesWindow = new QuickNotesWindow(_notesContent)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            _notesWindow.NotesChanged += text => _notesContent = text;
        }

        if (_notesWindow.IsVisible)
        {
            _notesWindow.Hide();
        }
        else
        {
            _notesWindow.Show();
            _notesWindow.Activate();
        }
    }

    private void CalculatorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_calculatorWindow is null)
        {
            _calculatorWindow = new CalculatorWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
        }

        if (_calculatorWindow.IsVisible)
        {
            _calculatorWindow.Hide();
        }
        else
        {
            _calculatorWindow.Show();
            _calculatorWindow.Activate();
        }
    }

    private async void WifiButton_Click(object sender, RoutedEventArgs e)
    {
        if (_wifiWindow is null)
        {
            _wifiWindow = new WifiFlyoutWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
        }

        if (_wifiWindow.IsVisible)
        {
            _wifiWindow.Hide();
        }
        else
        {
            _wifiWindow.Show();
            _wifiWindow.Activate();
            await _wifiWindow.RefreshNetworksAsync();
        }
    }

    private void EmergencyExitButton_Click(object sender, RoutedEventArgs e)
    {
        EmergencyExitDialog dialog = new() { Owner = this };
        if (dialog.ShowDialog() == true && dialog.EmergencyConfirmed)
        {
            SaveSessionHistory(completedNormally: false, usedEmergencyExit: true);
            DisarmStrictAndCleanup();
            CleanupAndReturnToDashboard();
        }
    }

    // -------------------------------------------------------------------------
    // Completion & Cleanup
    // -------------------------------------------------------------------------
    private void CompleteSessionNormally()
    {
        _sessionFinished = true;
        _mainTimer.Stop();

        SaveSessionHistory(completedNormally: true, usedEmergencyExit: false);
        DisarmStrictAndCleanup();
        _systemGuard.Disengage();
        CloseFloatingTools();

        // Hide WebViews cleanly so completion overlay renders unobstructed
        LeftWebHost.Children.Clear();
        RightWebHost.Children.Clear();

        TimeSpan studied = TimeSpan.FromSeconds(Math.Round(_elapsedStudySeconds));
        CompletedSummaryText.Text = $"{(int)studied.TotalHours:D2}:{studied.Minutes:D2}:{studied.Seconds:D2}";
        CompletedOverlay.Visibility = Visibility.Visible;

        try
        {
            SystemSounds.Exclamation.Play();
        }
        catch
        {
            // Ignore
        }
    }

    private void ReturnToDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        CleanupAndReturnToDashboard();
    }

    private void SaveSessionHistory(bool completedNormally, bool usedEmergencyExit)
    {
        double avgAfk = _antiAfkResponseTimes.Count > 0 ? _antiAfkResponseTimes.Average() : 0;

        StudyRecord record = new()
        {
            Id = _sessionId,
            StartTimeUtc = _sessionStartUtc,
            EndTimeUtc = DateTime.UtcNow,
            SiteName = string.IsNullOrWhiteSpace(_config.SiteTitle) ? _domainGuard.AllowedBaseDomain : _config.SiteTitle,
            SiteUrl = _config.TargetUrl,
            Mode = SessionMode.Study,
            LockLevel = _config.LockLevel,
            PlannedMinutes = _config.GetClampedDurationMinutes(),
            ActualStudySeconds = (int)Math.Round(_elapsedStudySeconds),
            CompletedNormally = completedNormally,
            UsedEmergencyExit = usedEmergencyExit,
            AntiAfkCheckCount = _antiAfkResponseTimes.Count,
            AverageAntiAfkResponseSeconds = Math.Round(avgAfk, 1),
            Notes = _notesContent.Trim()
        };

        App.StateService.AddHistoryRecord(record);
    }

    private void DisarmStrictAndCleanup()
    {
        _strictArmed = false;
        App.StateService.ClearStrictSession();
        PrivilegeAndStartupService.UnregisterStrictAutoStart();
    }

    private void CloseFloatingTools()
    {
        try { _notesWindow?.Close(); } catch { }
        try { _calculatorWindow?.Close(); } catch { }
        try { _wifiWindow?.Close(); } catch { }
        _notesWindow = null;
        _calculatorWindow = null;
        _wifiWindow = null;
    }

    private void CleanupAndReturnToDashboard()
    {
        _mainTimer.Stop();
        _toastTimer.Stop();
        CloseFloatingTools();
        _systemGuard.Dispose();
        _allowClose = true;

        MainWindow dashboard = new();
        Application.Current.MainWindow = dashboard;
        dashboard.Show();
        Close();
    }

    private void ShowBlockedToast(string message)
    {
        BlockedToastText.Text = message;
        BlockedToastBanner.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private static string TruncateTitle(string title) =>
        title.Length > 28 ? title[..28] + "…" : title;

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            return;
        }

        _mainTimer.Stop();
        _toastTimer.Stop();
        CloseFloatingTools();
        _systemGuard.Dispose();

        foreach (BrowserTabItem tab in _tabs)
        {
            try
            {
                tab.WebView?.Dispose();
            }
            catch
            {
                // Ignore disposal race
            }
        }
    }
}
