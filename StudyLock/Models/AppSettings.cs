namespace StudyLock.Models;

public sealed class AppSettings
{
    public string LanguageCode { get; set; } = "ar";
    public string ThemeMode { get; set; } = "Dark";
    public bool ParentPinEnabled { get; set; }
    public string ParentPinHash { get; set; } = string.Empty;
    public string ParentPinSalt { get; set; } = string.Empty;
    public string LastStudyUrl { get; set; } = "https://e-learning.qiyas.sa";
    public int LastStudyDurationMinutes { get; set; } = 60;
    public int LastChildDurationMinutes { get; set; } = 30;
    public LockLevel LastLockLevel { get; set; } = LockLevel.Standard;
    public List<string> OAuthWhitelistHosts { get; set; } = GetDefaultOAuthWhitelist();

    public static List<string> GetDefaultOAuthWhitelist() =>
    [
        "accounts.google.com",
        "accounts.youtube.com",
        "oauth2.googleapis.com",
        "login.microsoftonline.com",
        "login.live.com",
        "appleid.apple.com",
        "id.apple.com",
        "challenges.cloudflare.com",
        "auth0.com",
        "login.yahoo.com",
        "sso.nafath.sa",
        "iam.gov.sa",
        "nafath.sa",
        "github.com"
    ];
}
