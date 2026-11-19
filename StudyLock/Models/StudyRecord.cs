using System.Text.Json.Serialization;

namespace StudyLock.Models;

public sealed class StudyRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
    public DateTime EndTimeUtc { get; set; } = DateTime.UtcNow;
    public string SiteName { get; set; } = string.Empty;
    public string SiteUrl { get; set; } = string.Empty;
    public SessionMode Mode { get; set; } = SessionMode.Study;
    public LockLevel LockLevel { get; set; } = LockLevel.Standard;
    public int PlannedMinutes { get; set; }
    public int ActualStudySeconds { get; set; }
    public bool CompletedNormally { get; set; }
    public bool UsedEmergencyExit { get; set; }
    public int AntiAfkCheckCount { get; set; }
    public double AverageAntiAfkResponseSeconds { get; set; }
    public string Notes { get; set; } = string.Empty;

    [JsonIgnore]
    public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

    [JsonIgnore]
    public string LocalDateDisplay => StartTimeUtc.ToLocalTime().ToString("yyyy-MM-dd  HH:mm");

    [JsonIgnore]
    public string ActualDurationDisplay
    {
        get
        {
            TimeSpan ts = TimeSpan.FromSeconds(Math.Max(0, ActualStudySeconds));
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
            }
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }

    [JsonIgnore]
    public string AntiAfkDisplay => AntiAfkCheckCount > 0
        ? $"{AverageAntiAfkResponseSeconds:F1}s ({AntiAfkCheckCount})"
        : "—";
}
