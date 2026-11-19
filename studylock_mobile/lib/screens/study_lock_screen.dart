import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter_inappwebview/flutter_inappwebview.dart';
import 'package:syncfusion_flutter_pdfviewer/pdfviewer.dart';
import '../../core/localization/app_localizations.dart';
import '../../core/native/native_lock_bridge.dart';
import '../../core/theme/app_theme.dart';
import '../../models/session_config.dart';
import '../../models/study_record.dart';
import '../../services/domain_guard.dart';
import '../../services/storage_service.dart';
import '../../services/timer_engine.dart';
import 'components/afk_pulse_banner.dart';
import 'components/vertical_split_view.dart';
import 'emergency_exit_dialog.dart';

class StudyLockScreen extends StatefulWidget {
  final SessionConfig config;
  final StorageService storageService;
  final int? initialRemainingSeconds;

  const StudyLockScreen({
    super.key,
    required this.config,
    required this.storageService,
    this.initialRemainingSeconds,
  });

  @override
  State<StudyLockScreen> createState() => _StudyLockScreenState();
}

class _StudyLockScreenState extends State<StudyLockScreen> {
  late final TimerEngine _timerEngine;
  InAppWebViewController? _webViewController;
  int _activePdfIndex = 0;
  final TextEditingController _notesController = TextEditingController();

  @override
  void initState() {
    super.initState();

    final totalSeconds = widget.config.durationMinutes * 60;
    _timerEngine = TimerEngine(
      totalDurationSeconds: totalSeconds,
      isStrictMode: widget.config.isStrictMode,
      initialRemainingSeconds: widget.initialRemainingSeconds,
      onTickUpdate: (remaining) {
        if (widget.config.isStrictMode) {
          widget.storageService.updateRemainingSeconds(remaining);
        }
      },
      onCompleted: _handleSessionCompleted,
    );

    _timerEngine.start();
    _armNativeKiosk();
  }

  Future<void> _armNativeKiosk() async {
    await NativeLockBridge.startKioskLock(isStrict: widget.config.isStrictMode);
    await NativeLockBridge.enableKeepScreenOn(true);
  }

  Future<void> _disarmNativeKiosk() async {
    await NativeLockBridge.enableKeepScreenOn(false);
    await NativeLockBridge.stopKioskLock();
  }

  void _handleSessionCompleted() async {
    await _disarmNativeKiosk();
    if (widget.config.isStrictMode) {
      await widget.storageService.clearStrictState();
    }

    // Save study record
    final record = StudyRecord(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      timestamp: DateTime.now(),
      durationMinutes: widget.config.durationMinutes,
      mode: 'study',
      wasStrict: widget.config.isStrictMode,
      completedNaturally: true,
    );
    await widget.storageService.addStudyRecord(record);

    if (!mounted) return;

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        backgroundColor: AppTheme.bgSurface2,
        title: const Text('🎉 مبروك! اكتملت الجلسة بنجاح'),
        content: Text(
          'أتممت ${widget.config.durationMinutes} دقيقة من المذاكرة المركزة بدون أي مشتتات.',
          style: const TextStyle(color: AppTheme.textSecondary),
        ),
        actions: [
          ElevatedButton(
            onPressed: () {
              Navigator.of(context).pop(); // pop dialog
              Navigator.of(context).pop(); // exit lock screen
            },
            child: const Text('العودة للرئيسية'),
          ),
        ],
      ),
    );
  }

  void _confirmEmergencyExit() async {
    await _disarmNativeKiosk();
    if (widget.config.isStrictMode) {
      await widget.storageService.clearStrictState();
    }

    // Save incomplete record
    final record = StudyRecord(
      id: DateTime.now().millisecondsSinceEpoch.toString(),
      timestamp: DateTime.now(),
      durationMinutes: (_timerEngine.elapsedSeconds / 60).floor(),
      mode: 'study',
      wasStrict: widget.config.isStrictMode,
      completedNaturally: false,
    );
    await widget.storageService.addStudyRecord(record);

    if (!mounted) return;
    Navigator.of(context).pop();
  }

  void _openNotesSheet() {
    showModalBottomSheet(
      context: context,
      backgroundColor: AppTheme.bgSurface2,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => Padding(
        padding: EdgeInsets.only(
          bottom: MediaQuery.of(context).viewInsets.bottom + 16,
          left: 16,
          right: 16,
          top: 16,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                const Text(
                  'المفكرة السريعة للمذاكرة',
                  style: TextStyle(
                    color: AppTheme.textPrimary,
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, color: AppTheme.textMuted),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _notesController,
              maxLines: 8,
              style: const TextStyle(color: AppTheme.textPrimary),
              decoration: const InputDecoration(
                hintText: 'سجل ملاحظاتك وأفكارك السريعة هنا أثناء دراسة الدرس...',
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _timerEngine.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final hasPdfs = widget.config.pdfPaths.isNotEmpty;

    return WillPopScope(
      onWillPop: () async {
        // Intercept back button completely during lock session
        return false;
      },
      child: Scaffold(
        backgroundColor: AppTheme.bgCanvas,
        body: SafeArea(
          child: ListenableBuilder(
            listenable: _timerEngine,
            builder: (context, _) {
              return Stack(
                children: [
                  Column(
                    children: [
                      // Top Bar: Timer, Controls & Utility Actions
                      _buildTopHeaderBar(l10n),

                      // Progress Bar
                      LinearProgressIndicator(
                        value: _timerEngine.progressRatio,
                        backgroundColor: AppTheme.bgSurface3,
                        valueColor: const AlwaysStoppedAnimation<Color>(
                          AppTheme.accentPrimary,
                        ),
                        minHeight: 3,
                      ),

                      // Center Work Area (Split or Full Web)
                      Expanded(
                        child: hasPdfs
                            ? VerticalSplitView(
                                top: _buildWebViewPane(),
                                bottom: _buildPdfPane(),
                              )
                            : _buildWebViewPane(),
                      ),
                    ],
                  ),

                  // Floating AFK Pulse Alert
                  if (_timerEngine.state == TimerState.afkPrompt)
                    Positioned(
                      top: 60,
                      left: 0,
                      right: 0,
                      child: AfkPulseBanner(
                        countdownSeconds: _timerEngine.afkCountdownSeconds,
                        onConfirm: _timerEngine.confirmPresence,
                      ),
                    ),

                  // Fair Pause Screen Dimmer
                  if (_timerEngine.state == TimerState.fairPaused)
                    _buildFairPauseOverlay(l10n),
                ],
              );
            },
          ),
        ),
      ),
    );
  }

  Widget _buildTopHeaderBar(AppLocalizations l10n) {
    return Container(
      height: 58,
      padding: const EdgeInsets.symmetric(horizontal: 12),
      decoration: const BoxDecoration(
        color: AppTheme.bgSurface1,
        border: Border(
          bottom: BorderSide(color: AppTheme.borderSubtle),
        ),
      ),
      child: Row(
        children: [
          // Remaining Countdown Badge
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: AppTheme.accentSubtle,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: AppTheme.borderAccent),
            ),
            child: Row(
              children: [
                const Icon(
                  Icons.timer_outlined,
                  color: AppTheme.accentPrimary,
                  size: 18,
                ),
                const SizedBox(width: 8),
                Text(
                  _timerEngine.formattedRemainingTime,
                  style: const TextStyle(
                    color: AppTheme.accentPrimary,
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'monospace',
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),

          // Strict Mode Badge
          if (widget.config.isStrictMode)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: AppTheme.danger.withOpacity(0.15),
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Text(
                'صارم',
                style: TextStyle(
                  color: AppTheme.danger,
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),

          const Spacer(),

          // Fair Pause Toggle (Standard Mode Only)
          if (!widget.config.isStrictMode)
            IconButton(
              icon: Icon(
                _timerEngine.state == TimerState.fairPaused
                    ? Icons.play_arrow_rounded
                    : Icons.pause_rounded,
                color: AppTheme.textSecondary,
              ),
              onPressed: () {
                if (_timerEngine.state == TimerState.fairPaused) {
                  _timerEngine.resume();
                } else {
                  _timerEngine.manualPause();
                }
              },
              tooltip: l10n.translate('pause_button'),
            ),

          // In-Lock Notes Sheet
          IconButton(
            icon: const Icon(Icons.note_alt_outlined, color: AppTheme.textSecondary),
            onPressed: _openNotesSheet,
            tooltip: 'المفكرة',
          ),

          // Emergency Exit Button
          IconButton(
            icon: const Icon(Icons.exit_to_app_rounded, color: AppTheme.danger),
            onPressed: () {
              showDialog(
                context: context,
                builder: (context) => EmergencyExitDialog(
                  onEmergencyExitConfirmed: _confirmEmergencyExit,
                ),
              );
            },
            tooltip: l10n.translate('emergency_exit_title'),
          ),
        ],
      ),
    );
  }

  Widget _buildWebViewPane() {
    return InAppWebView(
      initialUrlRequest: URLRequest(
        url: WebUri(DomainGuard.normalizeUrl(widget.config.studyUrl)),
      ),
      initialSettings: InAppWebViewSettings(
        useHybridComposition: true,
        allowsInlineMediaPlayback: true,
        javaScriptEnabled: true,
        mediaPlaybackRequiresUserGesture: false,
        supportMultipleWindows: false,
        cacheEnabled: true,
        clearCache: false,
      ),
      onWebViewCreated: (controller) {
        _webViewController = controller;
      },
      shouldOverrideUrlLoading: (controller, navigationAction) async {
        final uri = navigationAction.request.url;
        if (uri == null) return NavigationActionPolicy.CANCEL;

        final isAllowed = DomainGuard.isUrlAllowed(
          uri.toString(),
          widget.config.studyUrl,
          allowOAuth: true,
        );

        if (isAllowed) {
          return NavigationActionPolicy.ALLOW;
        } else {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('تم حظر الرابط الخارجي للحفاظ على تركيزك: ${uri.host}'),
              duration: const Duration(seconds: 2),
              backgroundColor: AppTheme.danger,
            ),
          );
          return NavigationActionPolicy.CANCEL;
        }
      },
    );
  }

  Widget _buildPdfPane() {
    final pdfFiles = widget.config.pdfPaths;
    if (pdfFiles.isEmpty) return const SizedBox.shrink();

    final currentPdfPath = pdfFiles[_activePdfIndex];

    return Column(
      children: [
        // Tabs if multiple PDFs
        if (pdfFiles.length > 1)
          Container(
            height: 36,
            color: AppTheme.bgSurface1,
            child: ListView.builder(
              scrollDirection: Axis.horizontal,
              itemCount: pdfFiles.length,
              itemBuilder: (context, index) {
                final fileName = File(pdfFiles[index]).uri.pathSegments.last;
                final isSelected = index == _activePdfIndex;
                return GestureDetector(
                  onTap: () {
                    setState(() {
                      _activePdfIndex = index;
                    });
                  },
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 14),
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: isSelected ? AppTheme.bgSurface3 : Colors.transparent,
                      border: Border(
                        bottom: BorderSide(
                          color: isSelected ? AppTheme.accentPrimary : Colors.transparent,
                          width: 2,
                        ),
                      ),
                    ),
                    child: Text(
                      fileName,
                      style: TextStyle(
                        color: isSelected ? AppTheme.accentPrimary : AppTheme.textMuted,
                        fontSize: 12,
                        fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                      ),
                    ),
                  ),
                );
              },
            ),
          ),

        // PDF Viewer
        Expanded(
          child: SfPdfViewer.file(
            File(currentPdfPath),
            enableTextSelection: true,
          ),
        ),
      ],
    );
  }

  Widget _buildFairPauseOverlay(AppLocalizations l10n) {
    return Container(
      color: Colors.black.withOpacity(0.85),
      padding: const EdgeInsets.all(24),
      child: Center(
        child: Container(
          padding: const EdgeInsets.all(24),
          decoration: BoxDecoration(
            color: AppTheme.bgSurface2,
            borderRadius: BorderRadius.circular(20),
            border: Border.all(color: AppTheme.borderAccent),
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(
                Icons.pause_circle_filled_rounded,
                color: AppTheme.accentPrimary,
                size: 64,
              ),
              const SizedBox(height: 16),
              Text(
                l10n.translate('fair_pause_title'),
                style: const TextStyle(
                  color: AppTheme.textPrimary,
                  fontSize: 20,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                l10n.translate('fair_pause_desc'),
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppTheme.textSecondary, fontSize: 14),
              ),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _timerEngine.resume,
                icon: const Icon(Icons.play_arrow_rounded),
                label: Text(l10n.translate('resume_button')),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppTheme.accentPrimary,
                  foregroundColor: Colors.black,
                  padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 14),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
