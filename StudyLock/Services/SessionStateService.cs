using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StudyLock.Models;

namespace StudyLock.Services;

public sealed record StatisticsSummary(
    double TodayHours,
    double ThisWeekHours,
    double TotalHours,
    int CompletedSessionsCount,
    double AverageAntiAfkResponseSeconds);

public sealed class SessionStateService
{
    private static readonly byte[] DpapiEntropy = Encoding.UTF8.GetBytes("StudyLock.StrictSession.Entropy.v1");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _baseDirectory;
    private readonly string _settingsPath;
    private readonly string _presetsPath;
    private readonly string _historyPath;
    private readonly string _strictLockPath;
    private readonly string _pendingStrictPath;

    public SessionStateService(string? customBaseDirectory = null)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(customBaseDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudyLock")
            : customBaseDirectory;

        Directory.CreateDirectory(_baseDirectory);

        _settingsPath = Path.Combine(_baseDirectory, "settings.json");
        _presetsPath = Path.Combine(_baseDirectory, "presets.json");
        _historyPath = Path.Combine(_baseDirectory, "history.json");
        _strictLockPath = Path.Combine(_baseDirectory, "session.lock");
        _pendingStrictPath = Path.Combine(_baseDirectory, "pending_strict.json");
    }

    public string BaseDirectory => _baseDirectory;
    public string StudyWebViewProfilePath => Path.Combine(_baseDirectory, "WebView2Profile_Study");
    public string ChildWebViewProfilePath => Path.Combine(_baseDirectory, "WebView2Profile_Child");

    // -------------------------------------------------------------------------
    // App Settings
    // -------------------------------------------------------------------------
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = File.ReadAllText(_settingsPath, Encoding.UTF8);
                AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (loaded is not null)
                {
                    if (loaded.OAuthWhitelistHosts is null || loaded.OAuthWhitelistHosts.Count == 0)
                    {
                        loaded.OAuthWhitelistHosts = AppSettings.GetDefaultOAuthWhitelist();
                    }
                    return loaded;
                }
            }
        }
        catch
        {
            // Fallback to defaults if corrupted
        }

        AppSettings defaults = new();
        SaveSettings(defaults);
        return defaults;
    }

    public void SaveSettings(AppSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        AtomicWriteText(_settingsPath, json);
    }

    // -------------------------------------------------------------------------
    // Parent PIN Hashing (SHA-256 + 16-byte CSPRNG Salt)
    // -------------------------------------------------------------------------
    public static (string Hash, string Salt) CreatePinHash(string plainPin)
    {
        string normalized = MathChallengeService.ConvertArabicDigitsToAscii(plainPin.Trim());
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        byte[] hashBytes = ComputeSaltedHash(normalized, saltBytes);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public static bool VerifyParentPin(string plainPin, string storedHash, string storedSalt)
    {
        if (string.IsNullOrWhiteSpace(plainPin) ||
            string.IsNullOrWhiteSpace(storedHash) ||
            string.IsNullOrWhiteSpace(storedSalt))
        {
            return false;
        }

        try
        {
            string normalized = MathChallengeService.ConvertArabicDigitsToAscii(plainPin.Trim());
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] expectedHash = Convert.FromBase64String(storedHash);
            byte[] actualHash = ComputeSaltedHash(normalized, saltBytes);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] ComputeSaltedHash(string pin, byte[] salt)
    {
        byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
        byte[] combined = new byte[salt.Length + pinBytes.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(pinBytes, 0, combined, salt.Length, pinBytes.Length);
        return SHA256.HashData(combined);
    }

    // -------------------------------------------------------------------------
    // Preset Sites (Study & Child)
    // -------------------------------------------------------------------------
    public List<PresetSite> LoadPresets()
    {
        try
        {
            if (File.Exists(_presetsPath))
            {
                string json = File.ReadAllText(_presetsPath, Encoding.UTF8);
                List<PresetSite>? loaded = JsonSerializer.Deserialize<List<PresetSite>>(json, JsonOptions);
                if (loaded is not null)
                {
                    return loaded;
                }
            }
        }
        catch
        {
            // Fallback to defaults
        }

        List<PresetSite> defaults = CreateDefaultPresets();
        SavePresets(defaults);
        return defaults;
    }

    public void SavePresets(List<PresetSite> presets)
    {
        string json = JsonSerializer.Serialize(presets, JsonOptions);
        AtomicWriteText(_presetsPath, json);
    }

    public static List<PresetSite> CreateDefaultPresets() =>
    [
        // Study Mode Presets
        new PresetSite
        {
            Name = "قياس — تدريب القدرات",
            Url = "https://e-learning.qiyas.sa",
            IconGlyph = "🎯",
            AccentHex = "#F59E0B",
            Category = PresetCategory.Study
        },
        new PresetSite
        {
            Name = "منصة عين التعليمية",
            Url = "https://ien.edu.sa",
            IconGlyph = "📘",
            AccentHex = "#3B82F6",
            Category = PresetCategory.Study
        },
        new PresetSite
        {
            Name = "منصة مدرسة",
            Url = "https://madrasa.org",
            IconGlyph = "🎓",
            AccentHex = "#10B981",
            Category = PresetCategory.Study
        },
        new PresetSite
        {
            Name = "Khan Academy",
            Url = "https://www.khanacademy.org",
            IconGlyph = "📐",
            AccentHex = "#14B8A6",
            Category = PresetCategory.Study
        },
        new PresetSite
        {
            Name = "Coursera",
            Url = "https://www.coursera.org",
            IconGlyph = "💻",
            AccentHex = "#6366F1",
            Category = PresetCategory.Study
        },
        // Child Mode Presets
        new PresetSite
        {
            Name = "YouTube Kids",
            Url = "https://www.youtubekids.com",
            IconGlyph = "🎬",
            AccentHex = "#EF4444",
            Category = PresetCategory.Child
        },
        new PresetSite
        {
            Name = "منصة مدرسة للأطفال",
            Url = "https://madrasa.org",
            IconGlyph = "🌟",
            AccentHex = "#F59E0B",
            Category = PresetCategory.Child
        },
        new PresetSite
        {
            Name = "PBS Kids",
            Url = "https://pbskids.org",
            IconGlyph = "🧸",
            AccentHex = "#10B981",
            Category = PresetCategory.Child
        },
        new PresetSite
        {
            Name = "ABCya ألعاب ذكاء",
            Url = "https://www.abcya.com",
            IconGlyph = "🧩",
            AccentHex = "#3B82F6",
            Category = PresetCategory.Child
        },
        new PresetSite
        {
            Name = "Code.org برمجة للصغار",
            Url = "https://studio.code.org",
            IconGlyph = "🤖",
            AccentHex = "#8B5CF6",
            Category = PresetCategory.Child
        }
    ];

    // -------------------------------------------------------------------------
    // Study History & Statistics
    // -------------------------------------------------------------------------
    public List<StudyRecord> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyPath))
            {
                string json = File.ReadAllText(_historyPath, Encoding.UTF8);
                List<StudyRecord>? loaded = JsonSerializer.Deserialize<List<StudyRecord>>(json, JsonOptions);
                if (loaded is not null)
                {
                    return loaded.OrderByDescending(r => r.StartTimeUtc).ToList();
                }
            }
        }
        catch
        {
            // Ignore corrupted history
        }

        return [];
    }

    public void AddHistoryRecord(StudyRecord record)
    {
        List<StudyRecord> history = LoadHistory();
        history.Insert(0, record);
        string json = JsonSerializer.Serialize(history, JsonOptions);
        AtomicWriteText(_historyPath, json);
    }

    public static StatisticsSummary CalculateStatistics(IEnumerable<StudyRecord> records, DateTime? referenceLocalNow = null)
    {
        DateTime nowLocal = referenceLocalNow ?? DateTime.Now;
        DateTime todayStart = nowLocal.Date;
        DateTime weekStart = todayStart.AddDays(-6);

        List<StudyRecord> studyRecords = records.Where(r => r.Mode == SessionMode.Study).ToList();

        double todaySeconds = 0;
        double weekSeconds = 0;
        double totalSeconds = 0;
        int completedCount = 0;
        double weightedAfkSum = 0;
        int totalAfkChecks = 0;

        foreach (StudyRecord rec in studyRecords)
        {
            DateTime localStart = rec.StartTimeUtc.ToLocalTime();
            int sec = Math.Max(0, rec.ActualStudySeconds);
            totalSeconds += sec;

            if (localStart.Date == todayStart)
            {
                todaySeconds += sec;
            }

            if (localStart.Date >= weekStart)
            {
                weekSeconds += sec;
            }

            if (rec.CompletedNormally)
            {
                completedCount++;
            }

            if (rec.AntiAfkCheckCount > 0)
            {
                weightedAfkSum += rec.AverageAntiAfkResponseSeconds * rec.AntiAfkCheckCount;
                totalAfkChecks += rec.AntiAfkCheckCount;
            }
        }

        double avgAfk = totalAfkChecks > 0 ? weightedAfkSum / totalAfkChecks : 0;

        return new StatisticsSummary(
            TodayHours: todaySeconds / 3600.0,
            ThisWeekHours: weekSeconds / 3600.0,
            TotalHours: totalSeconds / 3600.0,
            CompletedSessionsCount: completedCount,
            AverageAntiAfkResponseSeconds: avgAfk);
    }

    // -------------------------------------------------------------------------
    // Strict Mode Encrypted Session Lock (Windows DPAPI)
    // -------------------------------------------------------------------------
    public bool HasActiveStrictSession()
    {
        StrictSessionState? state = LoadStrictSession();
        return state is not null && state.IsArmed && state.RemainingSeconds > 0;
    }

    public void SaveStrictSession(StrictSessionState state)
    {
        state.LastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string json = JsonSerializer.Serialize(state, JsonOptions);
        byte[] plainBytes = Encoding.UTF8.GetBytes(json);
        byte[] protectedBytes = ProtectedData.Protect(plainBytes, DpapiEntropy, DataProtectionScope.CurrentUser);
        AtomicWriteBytes(_strictLockPath, protectedBytes);
    }

    public StrictSessionState? LoadStrictSession()
    {
        try
        {
            if (!File.Exists(_strictLockPath))
            {
                return null;
            }

            byte[] protectedBytes = File.ReadAllBytes(_strictLockPath);
            byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, DpapiEntropy, DataProtectionScope.CurrentUser);
            string json = Encoding.UTF8.GetString(plainBytes);
            return JsonSerializer.Deserialize<StrictSessionState>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public void ClearStrictSession()
    {
        try
        {
            if (File.Exists(_strictLockPath))
            {
                File.Delete(_strictLockPath);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    // -------------------------------------------------------------------------
    // Pending Strict Handoff (for Hybrid UAC Elevation)
    // -------------------------------------------------------------------------
    public void SavePendingStrictConfig(SessionConfig config)
    {
        string json = JsonSerializer.Serialize(config, JsonOptions);
        AtomicWriteText(_pendingStrictPath, json);
    }

    public SessionConfig? ConsumePendingStrictConfig()
    {
        try
        {
            if (!File.Exists(_pendingStrictPath))
            {
                return null;
            }

            string json = File.ReadAllText(_pendingStrictPath, Encoding.UTF8);
            File.Delete(_pendingStrictPath);
            return JsonSerializer.Deserialize<SessionConfig>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static void AtomicWriteText(string targetPath, string content)
    {
        string tempPath = targetPath + ".tmp";
        File.WriteAllText(tempPath, content, Encoding.UTF8);
        File.Move(tempPath, targetPath, overwrite: true);
    }

    private static void AtomicWriteBytes(string targetPath, byte[] bytes)
    {
        string tempPath = targetPath + ".tmp";
        File.WriteAllBytes(tempPath, bytes);
        File.Move(tempPath, targetPath, overwrite: true);
    }
}
