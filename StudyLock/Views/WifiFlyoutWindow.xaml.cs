using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class WifiFlyoutWindow : Window
{
    private readonly WifiService _wifiService = new();
    private WifiNetworkInfo? _selectedNetwork;

    public WifiFlyoutWindow()
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        Loaded += async (_, _) => await RefreshNetworksAsync();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshNetworksAsync();
    }

    public async Task RefreshNetworksAsync()
    {
        RefreshButton.IsEnabled = false;
        StatusText.Text = LocalizationAndThemeService.GetString("WifiScanning", "Scanning Wi-Fi...");

        var (success, error, networks) = await _wifiService.ScanAndGetNetworksAsync();
        if (!success)
        {
            StatusText.Text = error ?? "Wi-Fi error.";
            NetworksListBox.ItemsSource = null;
        }
        else
        {
            NetworksListBox.ItemsSource = networks;
            StatusText.Text = $"{networks.Count} Wi-Fi";
        }

        RefreshButton.IsEnabled = true;
    }

    private void NetworksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedNetwork = NetworksListBox.SelectedItem as WifiNetworkInfo;
        if (_selectedNetwork is null)
        {
            ConnectPanel.Visibility = Visibility.Collapsed;
            return;
        }

        SelectedSsidText.Text = _selectedNetwork.Ssid;
        WifiPasswordBox.Clear();
        PasswordSection.Visibility = _selectedNetwork.IsSecured && !_selectedNetwork.HasSavedProfile
            ? Visibility.Visible
            : Visibility.Collapsed;
        ConnectPanel.Visibility = Visibility.Visible;
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedNetwork is null)
        {
            return;
        }

        ConnectButton.IsEnabled = false;
        string password = WifiPasswordBox.Password;
        var (success, error) = await _wifiService.ConnectAsync(_selectedNetwork, password);

        if (success)
        {
            StatusText.Text = $"✓ {_selectedNetwork.Ssid}";
            await Task.Delay(1800);
            await RefreshNetworksAsync();
        }
        else
        {
            StatusText.Text = error ?? "Failed to connect.";
            // If saved profile failed, show password box so user can re-enter
            if (_selectedNetwork.IsSecured)
            {
                PasswordSection.Visibility = Visibility.Visible;
            }
        }

        ConnectButton.IsEnabled = true;
    }
}
