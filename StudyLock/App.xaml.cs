using System.Windows;
using StudyLock.Models;
using StudyLock.Services;
using StudyLock.Views;

namespace StudyLock;

public partial class App : Application
{
    public static SessionStateService StateService { get; private set; } = null!;
    public static LocalizationAndThemeService Localization { get; private set; } = null!;
    public static WebViewEnvironmentService WebViewEnvironments { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        StateService = new SessionStateService();
        Localization = new LocalizationAndThemeService();
        WebViewEnvironments = new WebViewEnvironmentService(StateService);

        AppSettings settings = StateService.LoadSettings();
        Localization.Apply(settings.LanguageCode, settings.ThemeMode);

        bool isStrictLaunch = e.Args.Any(a => a.Equals("--launch-strict", StringComparison.OrdinalIgnoreCase));
        bool isStrictResume = e.Args.Any(a => a.Equals("--strict-resume", StringComparison.OrdinalIgnoreCase));

        // 1. Check for active encrypted Strict Mode recovery session (e.g., after reboot or crash)
        if (StateService.HasActiveStrictSession())
        {
            StrictSessionState? recovered = StateService.LoadStrictSession();
            if (recovered is not null)
            {
                // If launched manually as standard user while strict session is active, try elevating first
                if (!PrivilegeAndStartupService.IsRunningAsAdministrator() && !isStrictResume)
                {
                    if (PrivilegeAndStartupService.TryRelaunchAsAdministrator("--strict-resume"))
                    {
                        Shutdown();
                        return;
                    }
                }

                StudyLockWindow resumeWindow = new(recovered.Config, recovered);
                MainWindow = resumeWindow;
                resumeWindow.Show();
                return;
            }
        }

        // 2. Check for pending Strict Mode elevation handoff from Dashboard
        if (isStrictLaunch)
        {
            SessionConfig? pendingConfig = StateService.ConsumePendingStrictConfig();
            if (pendingConfig is not null)
            {
                StudyLockWindow strictWindow = new(pendingConfig);
                MainWindow = strictWindow;
                strictWindow.Show();
                return;
            }
        }

        // 3. Normal Dashboard startup
        MainWindow dashboard = new();
        MainWindow = dashboard;
        dashboard.Show();
    }
}
