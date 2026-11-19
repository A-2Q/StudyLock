using System.Windows;

namespace StudyLock.Services;

public sealed class LocalizationAndThemeService
{
    private const string ThemePrefix = "Themes/Theme.";
    private const string StringsPrefix = "Resources/Strings.";

    public string CurrentLanguage { get; private set; } = "ar";
    public string CurrentTheme { get; private set; } = "Dark";
    public FlowDirection CurrentFlowDirection =>
        CurrentLanguage.Equals("ar", StringComparison.OrdinalIgnoreCase)
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

    public event EventHandler? SettingsChanged;

    public void Apply(string languageCode, string themeMode)
    {
        CurrentLanguage = languageCode.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ar";
        CurrentTheme = themeMode.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";

        if (Application.Current is null)
        {
            return;
        }

        var merged = Application.Current.Resources.MergedDictionaries;

        // Swap Theme dictionary
        ReplaceDictionary(merged, ThemePrefix, $"Themes/Theme.{CurrentTheme}.xaml");

        // Swap Localization dictionary
        ReplaceDictionary(merged, StringsPrefix, $"Resources/Strings.{CurrentLanguage}.xaml");

        // Update FlowDirection on all open windows live
        foreach (Window window in Application.Current.Windows)
        {
            window.FlowDirection = CurrentFlowDirection;
        }

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public static string GetString(string key, string fallback = "")
    {
        if (Application.Current?.TryFindResource(key) is string found)
        {
            return found;
        }
        return fallback;
    }

    private static void ReplaceDictionary(IList<ResourceDictionary> merged, string prefix, string newRelativeUri)
    {
        ResourceDictionary? existing = null;
        int index = -1;

        for (int i = 0; i < merged.Count; i++)
        {
            string? src = merged[i].Source?.OriginalString;
            if (!string.IsNullOrEmpty(src) && src.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            {
                existing = merged[i];
                index = i;
                break;
            }
        }

        ResourceDictionary replacement = new()
        {
            Source = new Uri(newRelativeUri, UriKind.Relative)
        };

        if (existing is not null && index >= 0)
        {
            merged[index] = replacement;
        }
        else
        {
            merged.Insert(0, replacement);
        }
    }
}
