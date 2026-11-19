namespace StudyLock.Models;

public enum SessionMode
{
    Study,
    Child
}

public enum LockLevel
{
    Standard,
    Strict
}

public sealed class SessionConfig
{
    public const int MinDurationMinutes = 1;
    public const int MaxDurationMinutes = 300; // 5 hours hard cap

    public SessionMode Mode { get; set; } = SessionMode.Study;
    public string SiteTitle { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public List<string> AttachedPdfPaths { get; set; } = [];
    public int DurationMinutes { get; set; } = 60;
    public LockLevel LockLevel { get; set; } = LockLevel.Standard;
    public bool SkipGracePeriod { get; set; }

    public int GetClampedDurationMinutes()
    {
        return Math.Clamp(DurationMinutes, MinDurationMinutes, MaxDurationMinutes);
    }
}
