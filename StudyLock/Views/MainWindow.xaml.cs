using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class MainWindow : Window
{
    private AppSettings _settings;
    private List<PresetSite> _presets;
    private readonly List<string> _attachedPdfPaths = [];
    private PresetSite? _selectedChildPreset;
    private int _studyDurationMinutes = 60;
    private int _childDurationMinutes = 30;
    private bool _suppressDurationTextSync;

    public MainWindow()
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;

        _settings = App.StateService.LoadSettings();
        _presets = App.StateService.LoadPresets();

        StudyUrlTextBox.Text = _settings.LastStudyUrl;
        _studyDurationMinutes = Math.Clamp(_settings.LastStudyDurationMinutes, SessionConfig.MinDurationMinutes, SessionConfig.MaxDurationMinutes);
        _childDurationMinutes = Math.Clamp(_settings.LastChildDurationMinutes, SessionConfig.MinDurationMinutes, SessionConfig.MaxDurationMinutes);

        _suppressDurationTextSync = true;
        StudyCustomMinutesBox.Text = _studyDurationMinutes.ToString(CultureInfo.InvariantCulture);
        ChildCustomMinutesBox.Text = _childDurationMinutes.ToString(CultureInfo.InvariantCulture);
        _suppressDurationTextSync = false;

        SelectDurationChip(StudyDurationChipsPanel, _studyDurationMinutes);
        SelectDurationChip(ChildDurationChipsPanel, _childDurationMinutes);

        if (_settings.LastLockLevel == LockLevel.Strict)
        {
            StrictLevelRadio.IsChecked = true;
        }
        else
        {
            StandardLevelRadio.IsChecked = true;
        }

        UpdateThemeButtonLabel();
        RenderStudyPresets();
        RenderChildPresets();
        RenderAttachedPdfs();
        RefreshStatistics();
    }

    // -------------------------------------------------------------------------
    // Sidebar Navigation, Theme & Localization
    // -------------------------------------------------------------------------
    private void NavTab_Checked(object sender, RoutedEventArgs e)
    {
        if (StudyModePanel is null || ChildModePanel is null || StatisticsPanel is null)
        {
            return;
        }

        StudyModePanel.Visibility = NavStudyTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        ChildModePanel.Visibility = NavChildTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        StatisticsPanel.Visibility = NavStatsTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        if (NavStatsTab.IsChecked == true)
        {
            RefreshStatistics();
        }
    }

    private void LanguageToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.LanguageCode = _settings.LanguageCode.Equals("ar", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
        App.StateService.SaveSettings(_settings);
        App.Localization.Apply(_settings.LanguageCode, _settings.ThemeMode);
        UpdateThemeButtonLabel();
        RefreshStatistics();
    }

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemeMode = _settings.ThemeMode.Equals("Dark", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
        App.StateService.SaveSettings(_settings);
        App.Localization.Apply(_settings.LanguageCode, _settings.ThemeMode);
        UpdateThemeButtonLabel();
        RenderStudyPresets();
        RenderChildPresets();
        RefreshStatistics();
    }

    private void UpdateThemeButtonLabel()
    {
        bool isDark = _settings.ThemeMode.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        ThemeToggleButton.SetResourceReference(ContentProperty, isDark ? "ThemeLightLabel" : "ThemeDarkLabel");
    }

    private void ParentPinSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ParentPinSettingsDialog dialog = new(_settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            App.StateService.SaveSettings(_settings);
        }
    }

    private void DiscordButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.gg/PufJHjaAKJ",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore browser launch errors
        }
    }

    // -------------------------------------------------------------------------
    // Study Mode Presets & Attached PDFs
    // -------------------------------------------------------------------------
    private void RenderStudyPresets()
    {
        StudyPresetsPanel.Children.Clear();
        List<PresetSite> studySites = _presets.Where(p => p.Category == PresetCategory.Study).ToList();

        foreach (PresetSite site in studySites)
        {
            Border chip = new()
            {
                Background = (Brush)FindResource("BgSurface2Brush"),
                BorderBrush = (Brush)FindResource("BorderSubtleBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 7, 8, 7),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            StackPanel row = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(new TextBlock { Text = site.IconGlyph, FontSize = 14, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock
            {
                Text = site.Name,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });

            Button delBtn = new()
            {
                Content = "✕",
                Style = (Style)FindResource("GhostButtonStyle"),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(6, 0, 0, 0),
                FontSize = 10
            };
            delBtn.Click += (s, args) =>
            {
                args.Handled = true;
                _presets.Remove(site);
                App.StateService.SavePresets(_presets);
                RenderStudyPresets();
            };
            row.Children.Add(delBtn);

            chip.Child = row;
            chip.MouseLeftButtonUp += (_, _) =>
            {
                StudyUrlTextBox.Text = site.Url;
                StudyErrorBanner.Visibility = Visibility.Collapsed;
            };

            StudyPresetsPanel.Children.Add(chip);
        }
    }

    private void AddStudyPresetButton_Click(object sender, RoutedEventArgs e)
    {
        AddEditPresetDialog dialog = new(PresetCategory.Study) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultPreset is not null)
        {
            _presets.Add(dialog.ResultPreset);
            App.StateService.SavePresets(_presets);
            RenderStudyPresets();
            StudyUrlTextBox.Text = dialog.ResultPreset.Url;
        }
    }

    private void AttachPdfButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "PDF Documents (*.pdf)|*.pdf",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) == true)
        {
            foreach (string file in dialog.FileNames)
            {
                if (!_attachedPdfPaths.Contains(file, StringComparer.OrdinalIgnoreCase))
                {
                    _attachedPdfPaths.Add(file);
                }
            }
            RenderAttachedPdfs();
        }
    }

    private void RenderAttachedPdfs()
    {
        AttachedPdfsPanel.Children.Clear();
        NoPdfsHintText.Visibility = _attachedPdfPaths.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (string path in _attachedPdfPaths.ToList())
        {
            Border pill = new()
            {
                Background = (Brush)FindResource("AccentSubtleBrush"),
                BorderBrush = (Brush)FindResource("BorderAccentBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(10, 6, 8, 6),
                Margin = new Thickness(0, 0, 8, 8)
            };

            StackPanel row = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(new TextBlock { Text = "📄", Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center });
            row.Children.Add(new TextBlock
            {
                Text = Path.GetFileName(path),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                MaxWidth = 220,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            });

            Button removeBtn = new()
            {
                Content = "✕",
                Style = (Style)FindResource("GhostButtonStyle"),
                Padding = new Thickness(5, 1, 5, 1),
                Margin = new Thickness(6, 0, 0, 0),
                FontSize = 10
            };
            removeBtn.Click += (_, _) =>
            {
                _attachedPdfPaths.Remove(path);
                RenderAttachedPdfs();
            };

            row.Children.Add(removeBtn);
            pill.Child = row;
            AttachedPdfsPanel.Children.Add(pill);
        }
    }

    private void PreviewBeforeLockButton_Click(object sender, RoutedEventArgs e)
    {
        StudyErrorBanner.Visibility = Visibility.Collapsed;
        if (!DomainGuardService.TryValidateWebUrl(StudyUrlTextBox.Text, out Uri? validatedUri) || validatedUri is null)
        {
            ShowStudyError(LocalizationAndThemeService.GetString("InvalidUrlError", "Invalid URL."));
            return;
        }

        PreviewBrowserWindow preview = new(validatedUri.AbsoluteUri) { Owner = this };
        if (preview.ShowDialog() == true && preview.ConfirmedStartLock)
        {
            if (!string.IsNullOrWhiteSpace(preview.FinalUrl) &&
                DomainGuardService.TryValidateWebUrl(preview.FinalUrl, out _))
            {
                StudyUrlTextBox.Text = preview.FinalUrl;
            }
            LaunchStudySession(skipGrace: false);
        }
    }

    private void StartStudySessionButton_Click(object sender, RoutedEventArgs e)
    {
        LaunchStudySession(skipGrace: false);
    }

    private void LaunchStudySession(bool skipGrace)
    {
        StudyErrorBanner.Visibility = Visibility.Collapsed;

        if (!DomainGuardService.TryValidateWebUrl(StudyUrlTextBox.Text, out Uri? validatedUri) || validatedUri is null)
        {
            ShowStudyError(LocalizationAndThemeService.GetString("InvalidUrlError", "Invalid URL."));
            return;
        }

        if (!TryParseClampedDuration(StudyCustomMinutesBox.Text, out int minutes))
        {
            ShowStudyError(LocalizationAndThemeService.GetString("InvalidDurationError", "Duration must be 1–300 minutes."));
            return;
        }

        LockLevel level = StrictLevelRadio.IsChecked == true ? LockLevel.Strict : LockLevel.Standard;
        PresetSite? matchedPreset = _presets.FirstOrDefault(p =>
            p.Category == PresetCategory.Study &&
            DomainGuardService.ExtractRegistrableDomain(p.Url)
                .Equals(DomainGuardService.ExtractRegistrableDomain(validatedUri.AbsoluteUri), StringComparison.OrdinalIgnoreCase));

        SessionConfig config = new()
        {
            Mode = SessionMode.Study,
            SiteTitle = matchedPreset?.Name ?? validatedUri.Host,
            TargetUrl = validatedUri.AbsoluteUri,
            AttachedPdfPaths = [.. _attachedPdfPaths],
            DurationMinutes = minutes,
            LockLevel = level,
            SkipGracePeriod = skipGrace
        };

        // Persist user's latest preferences
        _settings.LastStudyUrl = validatedUri.AbsoluteUri;
        _settings.LastStudyDurationMinutes = minutes;
        _settings.LastLockLevel = level;
        App.StateService.SaveSettings(_settings);

        // Hybrid Privilege Model: elevate via UAC ONLY for Strict Mode
        if (level == LockLevel.Strict && !PrivilegeAndStartupService.IsRunningAsAdministrator())
        {
            App.StateService.SavePendingStrictConfig(config);
            bool relaunched = PrivilegeAndStartupService.TryRelaunchAsAdministrator("--launch-strict");
            if (relaunched)
            {
                Application.Current.Shutdown();
                return;
            }

            // User declined UAC — consume pending file and inform cleanly
            _ = App.StateService.ConsumePendingStrictConfig();
            ShowStudyError(LocalizationAndThemeService.GetString("UacDeclinedError", "Admin permission was declined for Strict Mode."));
            return;
        }

        StudyLockWindow lockWindow = new(config);
        Application.Current.MainWindow = lockWindow;
        lockWindow.Show();
        Close();
    }

    private void ShowStudyError(string message)
    {
        StudyErrorText.Text = message;
        StudyErrorBanner.Visibility = Visibility.Visible;
    }

    // -------------------------------------------------------------------------
    // Child Mode Presets Grid & Launch
    // -------------------------------------------------------------------------
    private void RenderChildPresets()
    {
        ChildPresetsPanel.Children.Clear();
        List<PresetSite> childSites = _presets.Where(p => p.Category == PresetCategory.Child).ToList();

        if (_selectedChildPreset is null || !childSites.Any(c => c.Id == _selectedChildPreset.Id))
        {
            _selectedChildPreset = childSites.FirstOrDefault();
        }

        foreach (PresetSite site in childSites)
        {
            bool isSelected = _selectedChildPreset?.Id == site.Id;
            Brush accentBrush = TryCreateBrush(site.AccentHex, (Brush)FindResource("AccentPrimaryBrush"));

            Border card = new()
            {
                Width = 185,
                Background = (Brush)FindResource(isSelected ? "AccentSubtleBrush" : "BgSurface2Brush"),
                BorderBrush = isSelected ? accentBrush : (Brush)FindResource("BorderSubtleBrush"),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 12, 12),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            Grid cardGrid = new();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Top Row: Icon Badge + Edit/Delete
            Grid topRow = new();
            Border iconBadge = new()
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(12),
                Background = accentBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                Child = new TextBlock
                {
                    Text = site.IconGlyph,
                    FontSize = 21,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            topRow.Children.Add(iconBadge);

            StackPanel actions = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            Button editBtn = new()
            {
                Content = "✎",
                Style = (Style)FindResource("GhostButtonStyle"),
                Padding = new Thickness(5, 2, 5, 2),
                FontSize = 11
            };
            editBtn.Click += (s, args) =>
            {
                args.Handled = true;
                AddEditPresetDialog dlg = new(PresetCategory.Child, site) { Owner = this };
                if (dlg.ShowDialog() == true && dlg.ResultPreset is not null)
                {
                    int idx = _presets.FindIndex(p => p.Id == site.Id);
                    if (idx >= 0)
                    {
                        _presets[idx] = dlg.ResultPreset;
                        App.StateService.SavePresets(_presets);
                        RenderChildPresets();
                    }
                }
            };
            Button delBtn = new()
            {
                Content = "✕",
                Style = (Style)FindResource("GhostButtonStyle"),
                Padding = new Thickness(5, 2, 5, 2),
                FontSize = 11
            };
            delBtn.Click += (s, args) =>
            {
                args.Handled = true;
                _presets.Remove(site);
                App.StateService.SavePresets(_presets);
                RenderChildPresets();
            };
            actions.Children.Add(editBtn);
            actions.Children.Add(delBtn);
            topRow.Children.Add(actions);

            Grid.SetRow(topRow, 0);
            cardGrid.Children.Add(topRow);

            TextBlock titleText = new()
            {
                Text = site.Name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 12, 0, 3)
            };
            Grid.SetRow(titleText, 1);
            cardGrid.Children.Add(titleText);

            TextBlock domainText = new()
            {
                Text = DomainGuardService.ExtractRegistrableDomain(site.Url),
                FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 11,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(domainText, 2);
            cardGrid.Children.Add(domainText);

            card.Child = cardGrid;
            card.MouseLeftButtonUp += (_, _) =>
            {
                _selectedChildPreset = site;
                ChildErrorBanner.Visibility = Visibility.Collapsed;
                RenderChildPresets();
            };

            ChildPresetsPanel.Children.Add(card);
        }
    }

    private void AddChildPresetButton_Click(object sender, RoutedEventArgs e)
    {
        AddEditPresetDialog dialog = new(PresetCategory.Child) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.ResultPreset is not null)
        {
            _presets.Add(dialog.ResultPreset);
            App.StateService.SavePresets(_presets);
            _selectedChildPreset = dialog.ResultPreset;
            RenderChildPresets();
        }
    }

    private void StartChildSessionButton_Click(object sender, RoutedEventArgs e)
    {
        ChildErrorBanner.Visibility = Visibility.Collapsed;

        if (_selectedChildPreset is null || !DomainGuardService.TryValidateWebUrl(_selectedChildPreset.Url, out Uri? validatedUri) || validatedUri is null)
        {
            ChildErrorText.Text = LocalizationAndThemeService.GetString("SelectChildSiteError", "Please select a child site.");
            ChildErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        if (!TryParseClampedDuration(ChildCustomMinutesBox.Text, out int minutes))
        {
            ChildErrorText.Text = LocalizationAndThemeService.GetString("InvalidDurationError", "Duration must be 1–300 minutes.");
            ChildErrorBanner.Visibility = Visibility.Visible;
            return;
        }

        _settings.LastChildDurationMinutes = minutes;
        App.StateService.SaveSettings(_settings);

        SessionConfig config = new()
        {
            Mode = SessionMode.Child,
            SiteTitle = _selectedChildPreset.Name,
            TargetUrl = validatedUri.AbsoluteUri,
            DurationMinutes = minutes,
            LockLevel = LockLevel.Standard
        };

        ChildLockWindow childWindow = new(config);
        Application.Current.MainWindow = childWindow;
        childWindow.Show();
        Close();
    }

    // -------------------------------------------------------------------------
    // Duration Chips & Custom Input Sync
    // -------------------------------------------------------------------------
    private void StudyDurationChip_Checked(object sender, RoutedEventArgs e)
    {
        if (StudyCustomMinutesBox is null || _suppressDurationTextSync)
        {
            return;
        }

        if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int mins))
        {
            _studyDurationMinutes = mins;
            _suppressDurationTextSync = true;
            StudyCustomMinutesBox.Text = mins.ToString(CultureInfo.InvariantCulture);
            _suppressDurationTextSync = false;
        }
    }

    private void StudyCustomMinutesBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressDurationTextSync)
        {
            return;
        }

        if (TryParseClampedDuration(StudyCustomMinutesBox.Text, out int mins))
        {
            _studyDurationMinutes = mins;
            _suppressDurationTextSync = true;
            SelectDurationChip(StudyDurationChipsPanel, mins);
            _suppressDurationTextSync = false;
        }
    }

    private void ChildDurationChip_Checked(object sender, RoutedEventArgs e)
    {
        if (ChildCustomMinutesBox is null || _suppressDurationTextSync)
        {
            return;
        }

        if (sender is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int mins))
        {
            _childDurationMinutes = mins;
            _suppressDurationTextSync = true;
            ChildCustomMinutesBox.Text = mins.ToString(CultureInfo.InvariantCulture);
            _suppressDurationTextSync = false;
        }
    }

    private void ChildCustomMinutesBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressDurationTextSync)
        {
            return;
        }

        if (TryParseClampedDuration(ChildCustomMinutesBox.Text, out int mins))
        {
            _childDurationMinutes = mins;
            _suppressDurationTextSync = true;
            SelectDurationChip(ChildDurationChipsPanel, mins);
            _suppressDurationTextSync = false;
        }
    }

    private static void SelectDurationChip(Panel panel, int minutes)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is RadioButton rb && int.TryParse(rb.Tag?.ToString(), out int tagVal))
            {
                rb.IsChecked = tagVal == minutes;
            }
        }
    }

    private static bool TryParseClampedDuration(string? raw, out int minutes)
    {
        minutes = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        string ascii = MathChallengeService.ConvertArabicDigitsToAscii(raw.Trim());
        return int.TryParse(ascii, out minutes) &&
               minutes is >= SessionConfig.MinDurationMinutes and <= SessionConfig.MaxDurationMinutes;
    }

    // -------------------------------------------------------------------------
    // Statistics & Session History
    // -------------------------------------------------------------------------
    private void RefreshStatistics()
    {
        List<StudyRecord> history = App.StateService.LoadHistory();
        StatisticsSummary summary = SessionStateService.CalculateStatistics(history);

        StatTodayValue.Text = summary.TodayHours.ToString("F1", CultureInfo.InvariantCulture);
        StatWeekValue.Text = summary.ThisWeekHours.ToString("F1", CultureInfo.InvariantCulture);
        StatTotalValue.Text = summary.TotalHours.ToString("F1", CultureInfo.InvariantCulture);
        StatAvgAfkValue.Text = summary.AverageAntiAfkResponseSeconds > 0
            ? summary.AverageAntiAfkResponseSeconds.ToString("F1", CultureInfo.InvariantCulture)
            : "—";

        HistoryListPanel.Children.Clear();
        NoHistoryText.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        foreach (StudyRecord rec in history.Take(50))
        {
            Border rowBorder = new()
            {
                Background = (Brush)FindResource("BgSurface2Brush"),
                BorderBrush = (Brush)FindResource("BorderSubtleBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 11, 14, 11),
                Margin = new Thickness(0, 0, 0, 8)
            };

            Grid row = new();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Site & Date
            StackPanel siteCol = new() { VerticalAlignment = VerticalAlignment.Center };
            siteCol.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(rec.SiteName) ? rec.SiteUrl : rec.SiteName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13.5,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            siteCol.Children.Add(new TextBlock
            {
                Text = rec.LocalDateDisplay,
                FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 11.5,
                Foreground = (Brush)FindResource("TextMutedBrush"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(siteCol, 0);
            row.Children.Add(siteCol);

            // Studied Duration
            StackPanel durCol = new() { VerticalAlignment = VerticalAlignment.Center };
            durCol.Children.Add(new TextBlock
            {
                Text = rec.ActualDurationDisplay,
                FontFamily = (FontFamily)FindResource("MonoFont"),
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = (Brush)FindResource("AccentPrimaryBrush")
            });
            durCol.Children.Add(new TextBlock
            {
                Text = rec.CompletedNormally
                    ? LocalizationAndThemeService.GetString("StatusCompleted", "Completed")
                    : LocalizationAndThemeService.GetString("StatusEmergency", "Emergency Exit"),
                FontSize = 11,
                Foreground = (Brush)FindResource(rec.CompletedNormally ? "SuccessBrush" : "DangerBrush")
            });
            Grid.SetColumn(durCol, 1);
            row.Children.Add(durCol);

            // Anti-AFK Speed
            TextBlock afkText = new()
            {
                Text = $"⚡ {rec.AntiAfkDisplay}",
                FontFamily = (FontFamily)FindResource("MonoFont"),
                FontSize = 12.5,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(afkText, 2);
            row.Children.Add(afkText);

            // Notes Button (if session has saved notes)
            if (rec.HasNotes)
            {
                Button notesBtn = new()
                {
                    Style = (Style)FindResource("SecondaryButtonStyle"),
                    Content = $"📝 {LocalizationAndThemeService.GetString("ViewNotesBtn", "Notes")}",
                    Padding = new Thickness(10, 6, 10, 6),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                notesBtn.Click += (_, _) =>
                {
                    SessionNotesViewerDialog dlg = new(rec) { Owner = this };
                    dlg.ShowDialog();
                };
                Grid.SetColumn(notesBtn, 3);
                row.Children.Add(notesBtn);
            }

            rowBorder.Child = row;
            HistoryListPanel.Children.Add(rowBorder);
        }
    }

    private static Brush TryCreateBrush(string hex, Brush fallback)
    {
        try
        {
            if (new BrushConverter().ConvertFromString(hex) is Brush b)
            {
                return b;
            }
        }
        catch
        {
            // Ignore malformed color
        }
        return fallback;
    }
}
