namespace StudyLock.Models;

public sealed class StrictSessionState
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public SessionConfig Config { get; set; } = new();
    public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
    public int RemainingSeconds { get; set; }
    public int ElapsedStudySeconds { get; set; }
    public int AntiAfkElapsedSeconds { get; set; }
    public List<double> AntiAfkResponseTimes { get; set; } = [];
    public string Notes { get; set; } = string.Empty;
    public bool IsArmed { get; set; }
    public long LastSavedUnixSeconds { get; set; }
}
