using System.IO;
using StudyLock.Models;
using StudyLock.Services;
using Xunit;

namespace StudyLock.Tests;

public sealed class SessionStateServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SessionStateService _service;

    public SessionStateServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "StudyLock_Tests_" + Guid.NewGuid().ToString("N"));
        _service = new SessionStateService(_tempDir);
    }

    [Fact]
    public void StrictSession_RoundTripsThroughDpapiEncryption()
    {
        StrictSessionState state = new()
        {
            SessionId = "sess-001",
            RemainingSeconds = 4200,
            ElapsedStudySeconds = 1800,
            Notes = "معادلات الجبر والهندسة",
            IsArmed = true,
            Config = new SessionConfig
            {
                Mode = SessionMode.Study,
                SiteTitle = "قياس",
                TargetUrl = "https://e-learning.qiyas.sa",
                DurationMinutes = 100,
                LockLevel = LockLevel.Strict
            }
        };

        _service.SaveStrictSession(state);
        Assert.True(_service.HasActiveStrictSession());

        // Ensure file on disk is NOT plaintext JSON
        string lockFilePath = Path.Combine(_tempDir, "session.lock");
        string rawText = File.ReadAllText(lockFilePath);
        Assert.DoesNotContain("e-learning.qiyas.sa", rawText);

        StrictSessionState? loaded = _service.LoadStrictSession();
        Assert.NotNull(loaded);
        Assert.Equal("sess-001", loaded!.SessionId);
        Assert.Equal(4200, loaded.RemainingSeconds);
        Assert.Equal("معادلات الجبر والهندسة", loaded.Notes);
        Assert.Equal(LockLevel.Strict, loaded.Config.LockLevel);

        _service.ClearStrictSession();
        Assert.False(_service.HasActiveStrictSession());
    }

    [Fact]
    public void ParentPin_HashesWithSaltAndVerifiesCorrectly()
    {
        var (hash, salt) = SessionStateService.CreatePinHash("4829");

        Assert.NotEqual("4829", hash);
        Assert.True(SessionStateService.VerifyParentPin("4829", hash, salt));
        Assert.True(SessionStateService.VerifyParentPin("٤٨٢٩", hash, salt));
        Assert.False(SessionStateService.VerifyParentPin("4820", hash, salt));
        Assert.False(SessionStateService.VerifyParentPin("", hash, salt));
    }

    [Fact]
    public void CalculateStatistics_AggregatesTodayWeekTotalAndAntiAfkSpeed()
    {
        DateTime nowLocal = new(2026, 11, 18, 15, 0, 0, DateTimeKind.Local);
        List<StudyRecord> records =
        [
            new StudyRecord
            {
                StartTimeUtc = nowLocal.AddHours(-2).ToUniversalTime(),
                ActualStudySeconds = 3600, // 1h today
                CompletedNormally = true,
                AntiAfkCheckCount = 2,
                AverageAntiAfkResponseSeconds = 4.0
            },
            new StudyRecord
            {
                StartTimeUtc = nowLocal.AddDays(-3).ToUniversalTime(),
                ActualStudySeconds = 7200, // 2h this week
                CompletedNormally = true,
                AntiAfkCheckCount = 4,
                AverageAntiAfkResponseSeconds = 7.0
            },
            new StudyRecord
            {
                StartTimeUtc = nowLocal.AddDays(-15).ToUniversalTime(),
                ActualStudySeconds = 1800, // 0.5h older
                CompletedNormally = false,
                AntiAfkCheckCount = 0
            }
        ];

        StatisticsSummary summary = SessionStateService.CalculateStatistics(records, nowLocal);

        Assert.Equal(1.0, summary.TodayHours, precision: 2);
        Assert.Equal(3.0, summary.ThisWeekHours, precision: 2);
        Assert.Equal(3.5, summary.TotalHours, precision: 2);
        Assert.Equal(2, summary.CompletedSessionsCount);
        // Weighted average: (2*4.0 + 4*7.0) / 6 = 36 / 6 = 6.0
        Assert.Equal(6.0, summary.AverageAntiAfkResponseSeconds, precision: 2);
    }

    [Fact]
    public void SessionConfig_ClampsDurationToFiveHoursMax()
    {
        SessionConfig cfgOver = new() { DurationMinutes = 999 };
        SessionConfig cfgUnder = new() { DurationMinutes = -5 };

        Assert.Equal(300, cfgOver.GetClampedDurationMinutes());
        Assert.Equal(1, cfgUnder.GetClampedDurationMinutes());
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Ignore temp cleanup
        }
    }
}
