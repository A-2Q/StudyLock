import 'package:flutter_test/flutter_test.dart';
import 'package:studylock_mobile/services/timer_engine.dart';

void main() {
  group('TimerEngine Unit Tests', () {
    test('Initializes formatted remaining time properly', () {
      final engine = TimerEngine(totalDurationSeconds: 1500); // 25 minutes
      expect(engine.formattedRemainingTime, '25:00');
      expect(engine.remainingSeconds, 1500);
      expect(engine.progressRatio, 0.0);
    });

    test('Initializes with custom initial remaining seconds for recovery', () {
      final engine = TimerEngine(
        totalDurationSeconds: 3600,
        initialRemainingSeconds: 1800,
      );
      expect(engine.remainingSeconds, 1800);
      expect(engine.formattedRemainingTime, '30:00');
      expect(engine.progressRatio, 0.5);
    });

    test('Formats multi-hour sessions correctly', () {
      final engine = TimerEngine(totalDurationSeconds: 7325); // 2h 2m 5s
      expect(engine.formattedRemainingTime, '02:02:05');
    });

    test('Manual pause and resume updates state in standard mode', () {
      final engine = TimerEngine(totalDurationSeconds: 600, isStrictMode: false);
      engine.start();
      expect(engine.state, TimerState.running);

      engine.manualPause();
      expect(engine.state, TimerState.fairPaused);

      engine.resume();
      expect(engine.state, TimerState.running);

      engine.dispose();
    });

    test('Manual pause is ignored in strict mode', () {
      final engine = TimerEngine(totalDurationSeconds: 600, isStrictMode: true);
      engine.start();
      expect(engine.state, TimerState.running);

      engine.manualPause();
      expect(engine.state, TimerState.running); // Should remain running in strict mode!

      engine.dispose();
    });
  });
}
