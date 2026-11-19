import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'core/localization/app_localizations.dart';
import 'core/theme/app_theme.dart';
import 'models/session_config.dart';
import 'screens/home_setup_screen.dart';
import 'screens/study_lock_screen.dart';
import 'services/storage_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  // Allow both portrait and landscape orientations across phones, foldables, and tablets
  await SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
    DeviceOrientation.landscapeLeft,
    DeviceOrientation.landscapeRight,
  ]);

  final storageService = await StorageService.initialize();

  // Check for interrupted strict session recovery
  final recoveredStrictState = storageService.getActiveStrictState();

  runApp(StudyLockMobileApp(
    storageService: storageService,
    recoveredStrictState: recoveredStrictState,
  ));
}

class StudyLockMobileApp extends StatefulWidget {
  final StorageService storageService;
  final dynamic recoveredStrictState;

  const StudyLockMobileApp({
    super.key,
    required this.storageService,
    this.recoveredStrictState,
  });

  @override
  State<StudyLockMobileApp> createState() => _StudyLockMobileAppState();
}

class _StudyLockMobileAppState extends State<StudyLockMobileApp> {
  late Locale _currentLocale;
  late bool _isDarkMode;

  @override
  void initState() {
    super.initState();
    _currentLocale = Locale(widget.storageService.getLocale());
    _isDarkMode = widget.storageService.isDarkMode();
  }

  void _updateLocale(Locale locale) {
    setState(() {
      _currentLocale = locale;
    });
  }

  void _updateThemeMode(bool isDark) {
    setState(() {
      _isDarkMode = isDark;
    });
  }

  @override
  Widget build(BuildContext context) {
    Widget homeWidget;

    if (widget.recoveredStrictState != null &&
        widget.recoveredStrictState.remainingSeconds > 0) {
      // Re-enter strict session lock immediately upon reboot/crash
      final recovered = widget.recoveredStrictState;
      final config = SessionConfig(
        studyUrl: recovered.studyUrl,
        pdfPaths: List<String>.from(recovered.pdfPaths),
        durationMinutes: (recovered.totalDurationSeconds / 60).round(),
        isStrictMode: true,
        mode: recovered.mode == 'child' ? SessionMode.child : SessionMode.study,
        parentPin: recovered.parentPin,
      );

      homeWidget = StudyLockScreen(
        config: config,
        storageService: widget.storageService,
        initialRemainingSeconds: recovered.remainingSeconds,
      );
    } else {
      homeWidget = HomeSetupScreen(
        storageService: widget.storageService,
        onLocaleChanged: _updateLocale,
        onThemeModeChanged: _updateThemeMode,
      );
    }

    return MaterialApp(
      title: 'StudyLock',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.lightTheme,
      darkTheme: AppTheme.darkTheme,
      themeMode: _isDarkMode ? ThemeMode.dark : ThemeMode.light,
      locale: _currentLocale,
      supportedLocales: const [
        Locale('ar'),
        Locale('en'),
      ],
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      home: homeWidget,
    );
  }
}
