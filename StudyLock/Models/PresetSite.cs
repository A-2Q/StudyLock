namespace StudyLock.Models;

public enum PresetCategory
{
    Study,
    Child
}

public sealed class PresetSite
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "📚";
    public string AccentHex { get; set; } = "#F59E0B";
    public PresetCategory Category { get; set; } = PresetCategory.Study;
}
