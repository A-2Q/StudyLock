using System.Windows;
using StudyLock.Models;

namespace StudyLock.Views;

public partial class PreviewBrowserWindow : Window
{
    private readonly string _initialUrl;

    public bool ConfirmedStartLock { get; private set; }
    public string FinalUrl { get; private set; }

    public PreviewBrowserWindow(string targetUrl)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        _initialUrl = targetUrl;
        FinalUrl = targetUrl;
        Loaded += PreviewBrowserWindow_Loaded;
    }

    private async void PreviewBrowserWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var env = await App.WebViewEnvironments.GetEnvironmentAsync(SessionMode.Study);
            await PreviewWebView.EnsureCoreWebView2Async(env);
            PreviewWebView.CoreWebView2.SourceChanged += (_, _) =>
            {
                if (!string.IsNullOrWhiteSpace(PreviewWebView.CoreWebView2.Source))
                {
                    FinalUrl = PreviewWebView.CoreWebView2.Source;
                }
            };
            PreviewWebView.CoreWebView2.Navigate(_initialUrl);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "WebView2 Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewWebView.CoreWebView2?.CanGoBack == true)
        {
            PreviewWebView.CoreWebView2.GoBack();
        }
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        PreviewWebView.CoreWebView2?.Reload();
    }

    private void ConfirmStartButton_Click(object sender, RoutedEventArgs e)
    {
        ConfirmedStartLock = true;
        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ConfirmedStartLock = false;
        DialogResult = false;
        Close();
    }
}
