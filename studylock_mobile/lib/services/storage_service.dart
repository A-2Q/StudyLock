import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/strict_state.dart';
import '../models/study_record.dart';

class StorageService {
  static const String keyStrictActive = 'strict_session_active';
  static const String keyRemainingSeconds = 'session_remaining_seconds';
  static const String keyStrictData = 'strict_session_json';
  static const String keyRecords = 'study_records_history';
  static const String keyLocale = 'selected_locale';
  static const String keyThemeMode = 'selected_theme_mode';
  static const String keyParentPin = 'default_parent_pin';

  final SharedPreferences prefs;

  StorageService(this.prefs);

  static Future<StorageService> initialize() async {
    final preferences = await SharedPreferences.getInstance();
    return StorageService(preferences);
  }

  // Strict Session Management for Boot/Crash Recovery
  Future<void> saveStrictState(StrictState state) async {
    await prefs.setBool(keyStrictActive, state.isActive);
    await prefs.setInt(keyRemainingSeconds, state.remainingSeconds);
    await prefs.setString(keyStrictData, jsonEncode(state.toJson()));
  }

  Future<void> updateRemainingSeconds(int seconds) async {
    await prefs.setInt(keyRemainingSeconds, seconds);
  }

  Future<void> clearStrictState() async {
    await prefs.setBool(keyStrictActive, false);
    await prefs.setInt(keyRemainingSeconds, 0);
    await prefs.remove(keyStrictData);
  }

  StrictState? getActiveStrictState() {
    final isActive = prefs.getBool(keyStrictActive) ?? false;
    final jsonStr = prefs.getString(keyStrictData);
    if (!isActive || jsonStr == null) return null;

    try {
      final map = jsonDecode(jsonStr) as Map<String, dynamic>;
      final state = StrictState.fromJson(map);
      // Synchronize with latest remaining seconds
      final currentRemaining = prefs.getInt(keyRemainingSeconds) ?? state.remainingSeconds;
      return StrictState(
        isActive: true,
        remainingSeconds: currentRemaining,
        totalDurationSeconds: state.totalDurationSeconds,
        studyUrl: state.studyUrl,
        pdfPaths: state.pdfPaths,
        mode: state.mode,
        parentPin: state.parentPin,
        lastSavedEpochMs: state.lastSavedEpochMs,
      );
    } catch (_) {
      return null;
    }
  }

  // Study Records History
  Future<void> addStudyRecord(StudyRecord record) async {
    final records = getStudyRecords();
    records.insert(0, record);
    final jsonList = records.map((r) => r.toJson()).toList();
    await prefs.setString(keyRecords, jsonEncode(jsonList));
  }

  List<StudyRecord> getStudyRecords() {
    final jsonStr = prefs.getString(keyRecords);
    if (jsonStr == null) return [];

    try {
      final list = jsonDecode(jsonStr) as List<dynamic>;
      return list.map((item) => StudyRecord.fromJson(item as Map<String, dynamic>)).toList();
    } catch (_) {
      return [];
    }
  }

  // Settings & Preferences
  String getLocale() => prefs.getString(keyLocale) ?? 'ar';
  Future<void> setLocale(String langCode) => prefs.setString(keyLocale, langCode);

  bool isDarkMode() => prefs.getBool(keyThemeMode) ?? true;
  Future<void> setDarkMode(bool isDark) => prefs.setBool(keyThemeMode, isDark);

  String? getParentPin() => prefs.getString(keyParentPin);
  Future<void> setParentPin(String pin) => prefs.setString(keyParentPin, pin);
}
