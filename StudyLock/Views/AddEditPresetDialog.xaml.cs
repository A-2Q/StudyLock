using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using StudyLock.Models;
using StudyLock.Services;

namespace StudyLock.Views;

public partial class AddEditPresetDialog : Window
{
    private static readonly string[] AvailableIcons =
        ["📚", "🎯", "📘", "🎓", "📐", "💻", "🎬", "🌟", "🧸", "🧩", "🤖", "🎨", "🔬", "🌍"];

    private static readonly string[] AvailableColors =
        ["#F59E0B", "#3B82F6", "#10B981", "#EF4444", "#8B5CF6", "#14B8A6", "#EC4899", "#F97316"];

    private readonly PresetCategory _category;
    private readonly string _existingId;
    private string _selectedIcon = "📚";
    private string _selectedColor = "#F59E0B";

    public PresetSite? ResultPreset { get; private set; }

    public AddEditPresetDialog(PresetCategory category, PresetSite? existing = null)
    {
        InitializeComponent();
        FlowDirection = App.Localization.CurrentFlowDirection;
        _category = category;
        _existingId = existing?.Id ?? Guid.NewGuid().ToString("N");

        if (existing is not null)
        {
            DialogHeaderText.SetResourceReference(TextBlock.TextProperty, "EditPresetTitle");
            NameTextBox.Text = existing.Name;
            UrlTextBox.Text = existing.Url;
            _selectedIcon = string.IsNullOrWhiteSpace(existing.IconGlyph) ? "📚" : existing.IconGlyph;
            _selectedColor = string.IsNullOrWhiteSpace(existing.AccentHex) ? "#F59E0B" : existing.AccentHex;
        }
        else if (category == PresetCategory.Child)
        {
            _selectedIcon = "🌟";
            _selectedColor = "#3B82F6";
        }

        BuildPickers();
        Loaded += (_, _) => NameTextBox.Focus();
    }

    private void BuildPickers()
    {
        IconPickerPanel.Children.Clear();
        foreach (string icon in AvailableIcons)
        {
            RadioButton rb = new()
            {
                Content = icon,
                GroupName = "IconGroup",
                IsChecked = icon == _selectedIcon,
                Style = (Style)FindResource("DurationChipStyle"),
                Padding = new Thickness(10, 6, 10, 6),
                FontSize = 16
            };
            rb.Checked += (_, _) => _selectedIcon = icon;
            IconPickerPanel.Children.Add(rb);
        }

        ColorPickerPanel.Children.Clear();
        foreach (string hex in AvailableColors)
        {
            Brush brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
            RadioButton rb = new()
            {
                GroupName = "ColorGroup",
                IsChecked = hex.Equals(_selectedColor, StringComparison.OrdinalIgnoreCase),
                Style = (Style)FindResource("DurationChipStyle"),
                Padding = new Thickness(8, 6, 8, 6),
                Content = new Border
                {
                    Width = 20,
                    Height = 20,
                    CornerRadius = new CornerRadius(10),
                    Background = brush
                }
            };
            rb.Checked += (_, _) => _selectedColor = hex;
            ColorPickerPanel.Children.Add(rb);
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        string name = NameTextBox.Text.Trim();
        string rawUrl = UrlTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || !DomainGuardService.TryValidateWebUrl(rawUrl, out Uri? validatedUri) || validatedUri is null)
        {
            ErrorText.Text = LocalizationAndThemeService.GetString("InvalidUrlError", "Please enter a valid site name and URL.");
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ResultPreset = new PresetSite
        {
            Id = _existingId,
            Name = name,
            Url = validatedUri.AbsoluteUri,
            IconGlyph = _selectedIcon,
            AccentHex = _selectedColor,
            Category = _category
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
