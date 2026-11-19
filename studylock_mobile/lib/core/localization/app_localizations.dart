import 'package:flutter/material.dart';

class AppLocalizations {
  final Locale locale;

  AppLocalizations(this.locale);

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations) ??
        AppLocalizations(const Locale('ar'));
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  static final Map<String, Map<String, String>> _localizedValues = {
    'ar': {
      'app_title': 'StudyLock',
      'tagline': 'بيئة مذاكرة مقفلة خالية من المشتتات',
      'tab_study': 'المذاكرة',
      'tab_child': 'وضع الطفل',
      'tab_analytics': 'الإحصائيات',
      'tab_settings': 'الإعدادات',
      'study_url_label': 'رابط موقع المذاكرة',
      'study_url_hint': 'https://khanacademy.org أو منصة دراستك',
      'study_url_required': 'يرجى إدخال رابط صالح لبدء الجلسة',
      'pdf_select_label': 'ملفات PDF المرفقة (اختياري)',
      'pdf_select_button': 'إضافة ملفات PDF',
      'pdf_none_selected': 'لم يتم اختيار ملفات، يمكنك الدراسة باستخدام الموقع فقط',
      'pdf_count': 'ملف محدد',
      'duration_label': 'مدة الجلسة',
      'minutes_short': 'دقيقة',
      'hours_short': 'ساعة',
      'strict_mode_title': 'المستوى الصارم (Strict Mode)',
      'strict_mode_desc': 'يمنع الخروج التام، يعيد تشغيل القفل تلقائياً بعد إعادة تشغيل الجوال، ويطلب رمزاً معقداً للخروج.',
      'standard_mode_desc': 'يوفر بيئة تركيز مقفلة مع إمكانية إيقاف مؤقت عادل للطوارئ.',
      'start_lock_button': 'بدء جلسة القفل الآن',
      'child_mode_title': 'وضع الطفل الآمن',
      'child_mode_desc': 'بوابة تعليمية وترفيهية آمنة ومقفلة للأطفال تمنع تصفح أي مواقع خارجية.',
      'child_preset_select': 'اختر المنصة التعليمية للطفل:',
      'parent_pin_label': 'رمز ولي الأمر (PIN للطوارئ)',
      'parent_pin_hint': '4 أرقام سرية',
      'start_child_lock': 'قفل التطبيق للطفل',
      'parent_gate_title': 'بوابة ولي الأمر',
      'parent_gate_prompt': 'أجب عن المسألة الحسابية لتأكيد الخروج:',
      'parent_gate_submit': 'تأكيد الخروج',
      'parent_gate_wrong': 'إجابة غير صحيحة!',
      'parent_gate_locked': 'تم قفل المحاولات لمدة دقيقتين لكثرة المحاولات الخاطئة.',
      'use_parent_pin': 'استخدام رمز PIN البديل',
      'emergency_exit_title': 'مخرج الطوارئ',
      'emergency_exit_prompt': 'اكتب الرمز العشوائي التالي يدوياً لفك القفل (النسخ واللصق معطل):',
      'emergency_exit_confirm': 'إنهاء الجلسة اضطرارياً',
      'afk_banner_title': 'تنبيه نبضة التركيز (15 دقيقة)',
      'afk_banner_desc': 'هل ما زلت تدرس؟ اضغط للتأكيد وتجنب تجميد المؤقت.',
      'afk_confirm_button': 'أنا هنا، استمر بالمؤقت',
      'fair_pause_title': 'المؤقت مجمد',
      'fair_pause_desc': 'تم تجميد المؤقت بشكل عادل وبدون أي عقوبة. اضغط استئناف عندما تعود لمذاكرتك.',
      'resume_button': 'استئناف المذاكرة',
      'pause_button': 'إيقاف مؤقت',
      'analytics_total_hours': 'إجمالي ساعات الإنجاز',
      'analytics_sessions_count': 'عدد الجلسات المكتملة',
      'analytics_strict_ratio': 'نسبة الالتزام بالوضع الصارم',
      'analytics_no_data': 'لا توجد جلسات مسجلة بعد. ابدأ أول جلسة مذاكرة الآن!',
      'permission_overlay_title': 'إذن الظهور فوق التطبيقات',
      'permission_overlay_desc': 'ضروري لمنع سحب شريط الإشعارات العلوي أثناء القفل.',
      'permission_access_title': 'خدمة إمكانية الوصول (Accessibility)',
      'permission_access_desc': 'تغلق شريط المهام والإعدادات فور محاولة فتحها.',
      'permission_battery_title': 'تجاهل تحسين البطارية',
      'permission_battery_desc': 'يضمن عدم إغلاق المؤقت في الخلفية لتوفير الشحن.',
      'permission_granted': 'مفعل',
      'permission_required': 'يتطلب التفعيل',
      'language_setting': 'لغة التطبيق',
      'theme_setting': 'المظهر',
      'dark_theme': 'داكن (OLED Charcoal)',
      'light_theme': 'فاتح (Warm Light)',
      'notes_sheet_title': 'المفكرة السريعة',
      'calculator_sheet_title': 'الحاسبة السريعة',
      'copyright_notice': 'Copyright © 2026 A-2Q - Apache License 2.0',
    },
    'en': {
      'app_title': 'StudyLock',
      'tagline': 'Distraction-Free Kiosk Study Environment',
      'tab_study': 'Study',
      'tab_child': 'Child Mode',
      'tab_analytics': 'Analytics',
      'tab_settings': 'Settings',
      'study_url_label': 'Study Website URL',
      'study_url_hint': 'https://khanacademy.org or your LMS platform',
      'study_url_required': 'Please enter a valid study URL to begin',
      'pdf_select_label': 'Attached PDF Files (Optional)',
      'pdf_select_button': 'Add PDF Documents',
      'pdf_none_selected': 'No files selected, you can study using the website alone',
      'pdf_count': 'file(s) selected',
      'duration_label': 'Session Duration',
      'minutes_short': 'min',
      'hours_short': 'hr',
      'strict_mode_title': 'Strict Mode',
      'strict_mode_desc': 'Blocks all exits, auto-resumes after phone reboots, and enforces a 55-char emergency exit string.',
      'standard_mode_desc': 'Provides a locked focus environment with a fair, penalty-free pause for emergencies.',
      'start_lock_button': 'Start Lock Session Now',
      'child_mode_title': 'Child Safe Kiosk',
      'child_mode_desc': 'Safe educational kiosk for children preventing external web access.',
      'child_preset_select': 'Select Child Learning Portal:',
      'parent_pin_label': 'Parent Security PIN',
      'parent_pin_hint': '4-digit secret PIN',
      'start_child_lock': 'Lock App for Child',
      'parent_gate_title': 'Parent Security Gate',
      'parent_gate_prompt': 'Solve this arithmetic challenge to exit:',
      'parent_gate_submit': 'Verify & Exit',
      'parent_gate_wrong': 'Incorrect answer!',
      'parent_gate_locked': 'Locked for 2 minutes due to 3 failed attempts.',
      'use_parent_pin': 'Use Backup PIN',
      'emergency_exit_title': 'Emergency Exit',
      'emergency_exit_prompt': 'Type the following 55-character code manually (Clipboard paste disabled):',
      'emergency_exit_confirm': 'Emergency Terminate',
      'afk_banner_title': 'Focus Pulse Check (15 Minutes)',
      'afk_banner_desc': 'Are you still studying? Tap to confirm and avoid timer freeze.',
      'afk_confirm_button': 'I am here, continue timer',
      'fair_pause_title': 'Timer Frozen',
      'fair_pause_desc': 'Timer paused fairly without penalty. Tap resume when you return to your studies.',
      'resume_button': 'Resume Study',
      'pause_button': 'Pause Session',
      'analytics_total_hours': 'Total Focused Hours',
      'analytics_sessions_count': 'Completed Sessions',
      'analytics_strict_ratio': 'Strict Mode Ratio',
      'analytics_no_data': 'No sessions recorded yet. Start your first focus session today!',
      'permission_overlay_title': 'System Alert Overlay',
      'permission_overlay_desc': 'Required to intercept and block notification shade pull-downs.',
      'permission_access_title': 'Accessibility Guard',
      'permission_access_desc': 'Suppresses system notification bar and quick settings bypasses.',
      'permission_battery_title': 'Ignore Battery Optimization',
      'permission_battery_desc': 'Guarantees the session watchdog runs without sleep interruption.',
      'permission_granted': 'Enabled',
      'permission_required': 'Grant Permission',
      'language_setting': 'Language',
      'theme_setting': 'Theme',
      'dark_theme': 'Dark (OLED Charcoal)',
      'light_theme': 'Light (Warm Light)',
      'notes_sheet_title': 'Quick Notes',
      'calculator_sheet_title': 'Quick Calculator',
      'copyright_notice': 'Copyright © 2026 A-2Q - Apache License 2.0',
    },
  };

  String translate(String key) {
    final lang = locale.languageCode;
    return _localizedValues[lang]?[key] ?? _localizedValues['ar']?[key] ?? key;
  }
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  bool isSupported(Locale locale) => ['ar', 'en'].contains(locale.languageCode);

  @override
  Future<AppLocalizations> load(Locale locale) async {
    return AppLocalizations(locale);
  }

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}
