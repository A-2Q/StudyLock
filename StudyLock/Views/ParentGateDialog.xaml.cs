using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class ParentGateDialog : Window
{
    private const int MaxWrongAttempts = 3;
    private const int LockoutDurationSeconds = 120; // 2 minutes

    private readonly MathChallengeService _mathService = new();
    private readonly AppSettings _settings;
    private readonly bool _usePinMode;
    private MathChallenge _currentChallenge;
    private int _wrongAttempts;
    private int _lockoutRemainingSeconds;
    private DispatcherTimer? _lockoutTimer;

    public bool IsUnlocked { get; private set; }

    public ParentGateDialog(AppSettings settings)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        _settings = settings;
        _usePinMode = settings.ParentPinEnabled &&
                      !string.IsNullOrWhiteSpace(settings.ParentPinHash) &&
                      !string.IsNullOrWhiteSpace(settings.ParentPinSalt);

        _currentChallenge = _mathService.GenerateNext();
        ConfigureModeUi();

        Loaded += (_, _) =>
        {
            if (_usePinMode)
            {
                PinPasswordBox.Focus();
            }
            else
            {
                AnswerTextBox.Focus();
            }
        };

        Closed += (_, _) => _lockoutTimer?.Stop();
    }

    private void ConfigureModeUi()
    {
        if (_usePinMode)
        {
            SubtitleText.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "ParentGatePinSubtitle");
            MathBox.Visibility = Visibility.Collapsed;
            AnswerTextBox.Visibility = Visibility.Collapsed;
            PinPasswordBox.Visibility = Visibility.Visible;
        }
        else
        {
            SubtitleText.SetResourceReference(System.Windows.Controls.TextBlock.TextProperty, "ParentGateMathSubtitle");
            MathBox.Visibility = Visibility.Visible;
            AnswerTextBox.Visibility = Visibility.Visible;
            PinPasswordBox.Visibility = Visibility.Collapsed;
            ExpressionText.Text = _currentChallenge.Expression;
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            VerifyAttempt();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void VerifyButton_Click(object sender, RoutedEventArgs e)
    {
        VerifyAttempt();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        IsUnlocked = false;
        DialogResult = false;
        Close();
    }

    private void VerifyAttempt()
    {
        if (_lockoutRemainingSeconds > 0)
        {
            return;
        }

        bool isCorrect = _usePinMode
            ? SessionStateService.VerifyParentPin(PinPasswordBox.Password, _settings.ParentPinHash, _settings.ParentPinSalt)
            : MathChallengeService.VerifyAnswer(_currentChallenge, AnswerTextBox.Text);

        if (isCorrect)
        {
            IsUnlocked = true;
            DialogResult = true;
            Close();
            return;
        }

        _wrongAttempts++;
        AnswerTextBox.Clear();
        PinPasswordBox.Clear();

        if (_wrongAttempts >= MaxWrongAttempts)
        {
            StartLockout();
        }
        else
        {
            int remaining = MaxWrongAttempts - _wrongAttempts;
            string prefix = LocalizationAndThemeService.GetString("ParentGateWrongAnswer", "Wrong answer! Remaining:");
            StatusText.Text = $"{prefix} {remaining}";
            StatusBanner.Visibility = Visibility.Visible;

            if (_usePinMode)
            {
                PinPasswordBox.Focus();
            }
            else
            {
                AnswerTextBox.Focus();
            }
        }
    }

    private void StartLockout()
    {
        _lockoutRemainingSeconds = LockoutDurationSeconds;
        AnswerTextBox.IsEnabled = false;
        PinPasswordBox.IsEnabled = false;
        VerifyButton.IsEnabled = false;
        StatusBanner.Visibility = Visibility.Visible;
        UpdateLockoutStatusText();

        _lockoutTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _lockoutTimer.Tick += (_, _) =>
        {
            _lockoutRemainingSeconds--;
            if (_lockoutRemainingSeconds <= 0)
            {
                _lockoutTimer.Stop();
                _wrongAttempts = 0;
                AnswerTextBox.IsEnabled = true;
                PinPasswordBox.IsEnabled = true;
                VerifyButton.IsEnabled = true;
                StatusBanner.Visibility = Visibility.Collapsed;

                if (!_usePinMode)
                {
                    _currentChallenge = _mathService.GenerateNext();
                    ExpressionText.Text = _currentChallenge.Expression;
                    AnswerTextBox.Focus();
                }
                else
                {
                    PinPasswordBox.Focus();
                }
            }
            else
            {
                UpdateLockoutStatusText();
            }
        };
        _lockoutTimer.Start();
    }

    private void UpdateLockoutStatusText()
    {
        TimeSpan ts = TimeSpan.FromSeconds(_lockoutRemainingSeconds);
        string prefix = LocalizationAndThemeService.GetString("ParentGateLockedOut", "Locked out:");
        StatusText.Text = $"{prefix} {ts:mm\\:ss}";
    }
}
