import 'package:flutter_test/flutter_test.dart';
import 'package:studylock_mobile/services/math_gate_service.dart';

void main() {
  group('MathGateService Unit Tests', () {
    late MathGateService gateService;

    setUp(() {
      gateService = MathGateService();
    });

    test('Generates challenge with questionText and correctAnswer', () {
      final challenge = gateService.generateTensAndFivesChallenge();
      expect(challenge.questionText.isNotEmpty, isTrue);
      expect(challenge.questionText.contains('='), isTrue);
      expect(challenge.questionText.contains('?'), isTrue);
    });

    test('Verifies correct answer and resets failed attempts', () {
      final challenge = gateService.generateTensAndFivesChallenge();
      final result = gateService.verifyAnswer(challenge.correctAnswer, challenge.correctAnswer);
      expect(result, isTrue);
      expect(gateService.failedAttempts, 0);
      expect(gateService.isLockedOut, isFalse);
    });

    test('Increments failed attempts on wrong answer', () {
      final challenge = gateService.generateTensAndFivesChallenge();
      final wrongAnswer = challenge.correctAnswer + 999;
      final result = gateService.verifyAnswer(wrongAnswer, challenge.correctAnswer);
      expect(result, isFalse);
      expect(gateService.failedAttempts, 1);
      expect(gateService.isLockedOut, isFalse);
    });

    test('Locks out for 2 minutes after 3 consecutive failed attempts', () {
      final challenge = gateService.generateTensAndFivesChallenge();
      final wrongAnswer = challenge.correctAnswer + 999;

      gateService.verifyAnswer(wrongAnswer, challenge.correctAnswer); // attempt 1
      expect(gateService.isLockedOut, isFalse);

      gateService.verifyAnswer(wrongAnswer, challenge.correctAnswer); // attempt 2
      expect(gateService.isLockedOut, isFalse);

      gateService.verifyAnswer(wrongAnswer, challenge.correctAnswer); // attempt 3
      expect(gateService.isLockedOut, isTrue);
      expect(gateService.remainingLockoutSeconds, greaterThan(100));

      // Attempt during lockout should be rejected immediately
      final attemptDuringLockout = gateService.verifyAnswer(challenge.correctAnswer, challenge.correctAnswer);
      expect(attemptDuringLockout, isFalse);
    });

    test('Verifies valid backup PIN', () {
      const pin = '4321';
      expect(gateService.verifyPin('4321', pin), isTrue);
      expect(gateService.verifyPin('0000', pin), isFalse);
    });
  });
}
