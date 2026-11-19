import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:vibration/vibration.dart';

enum TimerState {
  running,
  afkPrompt,
  fairPaused,
  completed,
}

class TimerEngine extends ChangeNotifier {
  final int totalDurationSeconds;
  final bool isStrictMode;
  final Function(int remainingSeconds)? onTickUpdate;
  final VoidCallback? onCompleted;

  late int remainingSeconds;
  int elapsedSeconds = 0;
  TimerState state = TimerState.running;

  Timer? _ticker;
  final Stopwatch _stopwatch = Stopwatch();
  int _lastStopwatchMs = 0;

  // 15-Minute Anti-AFK Pulse State
  static const int afkPulseIntervalSeconds = 15 * 60; // 15 minutes
  static const int afkPromptDurationSeconds = 60; // 60 seconds to confirm
  int afkCountdownSeconds = afkPromptDurationSeconds;
  Timer? _afkCountdownTimer;

  TimerEngine({
    required this.totalDurationSeconds,
    this.isStrictMode = false,
    int? initialRemainingSeconds,
    this.onTickUpdate,
    this.onCompleted,
  }) {
    remainingSeconds = initialRemainingSeconds ?? totalDurationSeconds;
  }

  void start() {
    state = TimerState.running;
    _stopwatch.start();
    _lastStopwatchMs = _stopwatch.elapsedMilliseconds;

    _ticker?.cancel();
    _ticker = Timer.periodic(const Duration(seconds: 1), _handleTick);
    notifyListeners();
  }

  void _handleTick(Timer timer) {
    if (state != TimerState.running) return;

    final currentMs = _stopwatch.elapsedMilliseconds;
    final deltaSeconds = ((currentMs - _lastStopwatchMs) / 1000).floor();

    if (deltaSeconds >= 1) {
      _lastStopwatchMs = currentMs;
      remainingSeconds -= deltaSeconds;
      elapsedSeconds += deltaSeconds;

      if (remainingSeconds <= 0) {
        remainingSeconds = 0;
        _completeSession();
        return;
      }

      onTickUpdate?.call(remainingSeconds);

      // Check 15-minute anti-AFK pulse trigger
      if (elapsedSeconds > 0 && elapsedSeconds % afkPulseIntervalSeconds == 0) {
        _triggerAfkPulse();
      }

      notifyListeners();
    }
  }

  Future<void> _triggerAfkPulse() async {
    state = TimerState.afkPrompt;
    afkCountdownSeconds = afkPromptDurationSeconds;

    // Trigger haptic vibration
    try {
      final hasVibrator = await Vibration.hasVibrator();
      if (hasVibrator == true) {
        Vibration.vibrate(pattern: [0, 250, 150, 250]);
      }
    } catch (_) {}

    notifyListeners();

    _afkCountdownTimer?.cancel();
    _afkCountdownTimer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (state != TimerState.afkPrompt) {
        timer.cancel();
        return;
      }

      afkCountdownSeconds--;
      if (afkCountdownSeconds <= 0) {
        timer.cancel();
        // Fair Pause without penalty
        _enterFairPause();
      }
      notifyListeners();
    });
  }

  void confirmPresence() {
    if (state == TimerState.afkPrompt) {
      _afkCountdownTimer?.cancel();
      state = TimerState.running;
      _lastStopwatchMs = _stopwatch.elapsedMilliseconds;
      notifyListeners();
    }
  }

  void _enterFairPause() {
    state = TimerState.fairPaused;
    _stopwatch.stop();
    notifyListeners();
  }

  void manualPause() {
    if (state == TimerState.running && !isStrictMode) {
      state = TimerState.fairPaused;
      _stopwatch.stop();
      notifyListeners();
    }
  }

  void resume() {
    if (state == TimerState.fairPaused) {
      state = TimerState.running;
      _stopwatch.start();
      _lastStopwatchMs = _stopwatch.elapsedMilliseconds;
      notifyListeners();
    }
  }

  void _completeSession() {
    state = TimerState.completed;
    _ticker?.cancel();
    _afkCountdownTimer?.cancel();
    _stopwatch.stop();
    notifyListeners();
    onCompleted?.call();
  }

  String get formattedRemainingTime {
    final hours = remainingSeconds ~/ 3600;
    final minutes = (remainingSeconds % 3600) ~/ 60;
    final seconds = remainingSeconds % 60;

    if (hours > 0) {
      return '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
    }
    return '${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }

  double get progressRatio {
    if (totalDurationSeconds <= 0) return 0.0;
    final ratio = 1.0 - (remainingSeconds / totalDurationSeconds);
    return ratio.clamp(0.0, 1.0);
  }

  @override
  void dispose() {
    _ticker?.cancel();
    _afkCountdownTimer?.cancel();
    _stopwatch.stop();
    super.dispose();
  }
}
