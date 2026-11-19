using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Web.WebView2.Wpf;

namespace StudyLock.Models;

public enum BrowserTabKind
{
    Web,
    Pdf
}

public sealed class BrowserTabItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _currentUrl = string.Empty;
    private bool _isActive;

    public string Id { get; } = Guid.NewGuid().ToString("N");
    public BrowserTabKind Kind { get; init; } = BrowserTabKind.Web;
    public bool IsPrimary { get; init; }
    public bool CanClose => !IsPrimary && Kind == BrowserTabKind.Web;
    public WebView2? WebView { get; set; }

    public string IconGlyph => Kind == BrowserTabKind.Pdf ? "📄" : "🌐";

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public string CurrentUrl
    {
        get => _currentUrl;
        set
        {
            if (_currentUrl != value)
            {
                _currentUrl = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
