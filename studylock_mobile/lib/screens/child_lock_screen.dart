import 'package:flutter/material.dart';
import 'package:flutter_inappwebview/flutter_inappwebview.dart';
import '../../core/localization/app_localizations.dart';
import '../../core/native/native_lock_bridge.dart';
import '../../core/theme/app_theme.dart';
import '../../models/session_config.dart';
import '../../models/study_record.dart';
import '../../services/domain_guard.dart';
import '../../services/math_gate_service.dart';
import '../../services/storage_service.dart';
import '../../services/timer_engine.dart';
import 'components/parent_gate_dialog.dart';

class ChildLockScreen extends StatefulWidget {
  final SessionConfig config;
  final StorageService storageService;

  const ChildLockScreen({
    super.key,
    required this.config,
    required this.storageService,
  });

  @override
  State<ChildLockScreen> createState() => _ChildLockScreenState();
}

class _ChildLockScreenState extends State<ChildLockScreen> {
  late final TimerEngine _timerEngine;
  final MathGateService _gateService = MathGateService();
  bool _isTimeUp = false;

  @override
  void initState() {
    super.initState();

    final totalSeconds = widget.config.durationMinutes * 60;
    _timerEngine = TimerEngine(
      totalDurationSeconds: totalSeconds,
      isStrictMode: true, // Child mode is always strictly enforced
      onCompleted: _handleChildTimeUp,
    );

    _timerEngine.start();
    _armKiosk();
  }

  Future<void> _armKiosk() async {
    await NativeLockBridge.startKioskLock(isStrict: true);
    await NativeLockBridge.enableKeepScreenOn(true);
  }

  Future<void> _disarmKiosk() async {
    await NativeLockBridge.enableKeepScreenOn(false);
    await NativeLockBridge.stopKioskLock();
  }

  void _handleChildTimeUp() {
    setState(() {
      _isTimeUp = true;
    });
  }

  void _openParentGate() {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => ParentGateDialog(
        gateService: _gateService,
        parentPin: widget.config.parentPin,
        onUnlockSuccess: _unlockAndExit,
      ),
    );
  }

  void _unlockAndExit() async {
    await _disarmKiosk();

    // Record record
    final record = StudyRecord(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      timestamp: DateTime.now(),
      durationMinutes: (_timerEngine.elapsedSeconds / 60).floor(),
      mode: 'child',
      wasStrict: true,
      completedNaturally: _isTimeUp,
    );
    await widget.storageService.addStudyRecord(record);

    if (!mounted) return;
    Navigator.of(context).pop();
  }

  @override
  void dispose() {
    _timerEngine.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return WillPopScope(
      onWillPop: () async => false,
      child: Scaffold(
        backgroundColor: AppTheme.bgCanvas,
        body: SafeArea(
          child: Stack(
            children: [
              Column(
                children: [
                  // Child Header Bar
                  Container(
                    height: 54,
                    padding: const EdgeInsets.symmetric(horizontal: 16),
                    color: AppTheme.bgSurface1,
                    child: Row(
                      children: [
                        // Child icon & title
                        const Icon(Icons.child_care_rounded, color: AppTheme.accentPrimary),
                        const SizedBox(width: 8),
                        Text(
                          l10n.translate('child_mode_title'),
                          style: const TextStyle(
                            color: AppTheme.textPrimary,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const Spacer(),

                        // Remaining Time
                        ListenableBuilder(
                          listenable: _timerEngine,
                          builder: (context, _) {
                            return Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                              decoration: BoxDecoration(
                                color: AppTheme.accentSubtle,
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Text(
                                _timerEngine.formattedRemainingTime,
                                style: const TextStyle(
                                  color: AppTheme.accentPrimary,
                                  fontWeight: FontWeight.bold,
                                  fontFamily: 'monospace',
                                ),
                              ),
                            );
                          },
                        ),
                        const SizedBox(width: 12),

                        // Parent Exit Gate Button
                        ElevatedButton.icon(
                          onPressed: _openParentGate,
                          icon: const Icon(Icons.lock_open_rounded, size: 16),
                          label: const Text('خروج الوالدين'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppTheme.bgSurface3,
                            foregroundColor: AppTheme.textPrimary,
                            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                            textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                          ),
                        ),
                      ],
                    ),
                  ),

                  // Child WebView
                  Expanded(
                    child: InAppWebView(
                      initialUrlRequest: URLRequest(
                        url: WebUri(DomainGuard.normalizeUrl(widget.config.studyUrl)),
                      ),
                      initialSettings: InAppWebViewSettings(
                        useHybridComposition: true,
                        allowsInlineMediaPlayback: true,
                        javaScriptEnabled: true,
                        supportMultipleWindows: false,
                        cacheEnabled: true,
                      ),
                      shouldOverrideUrlLoading: (controller, action) async {
                        final uri = action.request.url;
                        if (uri == null) return NavigationActionPolicy.CANCEL;

                        // Child mode strictly enforces origin matching, no random browsing
                        if (DomainGuard.isUrlAllowed(uri.toString(), widget.config.studyUrl, allowOAuth: false)) {
                          return NavigationActionPolicy.ALLOW;
                        }
                        return NavigationActionPolicy.CANCEL;
                      },
                    ),
                  ),
                ],
              ),

              // Time Up Overlay Screen
              if (_isTimeUp)
                Container(
                  color: Colors.black.withOpacity(0.92),
                  padding: const EdgeInsets.all(24),
                  child: Center(
                    child: Container(
                      padding: const EdgeInsets.all(28),
                      decoration: BoxDecoration(
                        color: AppTheme.bgSurface2,
                        borderRadius: BorderRadius.circular(24),
                        border: Border.all(color: AppTheme.accentPrimary),
                      ),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Icon(Icons.stars_rounded, color: AppTheme.accentPrimary, size: 72),
                          const SizedBox(height: 16),
                          const Text(
                            'انتهى وقت التعلم الممتع!',
                            style: TextStyle(
                              color: AppTheme.textPrimary,
                              fontSize: 22,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(height: 8),
                          const Text(
                            'أحسنت يا بطل! تم إنهاء الوقت المحدد لهذا اليوم.',
                            textAlign: TextAlign.center,
                            style: TextStyle(color: AppTheme.textSecondary, fontSize: 14),
                          ),
                          const SizedBox(height: 24),
                          ElevatedButton.icon(
                            onPressed: _openParentGate,
                            icon: const Icon(Icons.lock_open_rounded),
                            label: const Text('فتح الجهاز (ولي الأمر)'),
                            style: ElevatedButton.styleFrom(
                              backgroundColor: AppTheme.accentPrimary,
                              foregroundColor: Colors.black,
                              padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 14),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
