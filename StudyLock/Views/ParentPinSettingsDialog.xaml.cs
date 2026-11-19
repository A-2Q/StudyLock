using System.Windows;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class ParentPinSettingsDialog : Window
{
    private readonly AppSettings _settings;

    public ParentPinSettingsDialog(AppSettings settings)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        _settings = settings;
        EnablePinCheckBox.IsChecked = settings.ParentPinEnabled;
        PinFieldsPanel.Visibility = settings.ParentPinEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void EnablePinCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        bool enabled = EnablePinCheckBox.IsChecked == true;
        PinFieldsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        bool enablePin = EnablePinCheckBox.IsChecked == true;
        if (!enablePin)
        {
            _settings.ParentPinEnabled = false;
            DialogResult = true;
            Close();
            return;
        }

        string pin1 = MathChallengeService.ConvertArabicDigitsToAscii(NewPinBox.Password.Trim());
        string pin2 = MathChallengeService.ConvertArabicDigitsToAscii(ConfirmPinBox.Password.Trim());

        // Allow keeping existing PIN if already set and both fields left empty
        if (string.IsNullOrEmpty(pin1) && string.IsNullOrEmpty(pin2) &&
            !string.IsNullOrWhiteSpace(_settings.ParentPinHash))
        {
            _settings.ParentPinEnabled = true;
            DialogResult = true;
            Close();
            return;
        }

        bool validDigits = pin1.Length is >= 4 and <= 8 && pin1.All(char.IsAsciiDigit);
        if (!validDigits || pin1 != pin2)
        {
            ErrorText.Text = LocalizationAndThemeService.GetString("PinValidationError", "PIN must be 4-8 digits and match.");
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var (hash, salt) = SessionStateService.CreatePinHash(pin1);
        _settings.ParentPinEnabled = true;
        _settings.ParentPinHash = hash;
        _settings.ParentPinSalt = salt;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
