import 'dart:math';

class MathChallenge {
  final String questionText;
  final int correctAnswer;

  const MathChallenge({
    required this.questionText,
    required this.correctAnswer,
  });
}

class MathGateService {
  final Random _random = Random();
  int failedAttempts = 0;
  DateTime? lockoutUntil;

  static const int maxFailedAttempts = 3;
  static const Duration lockoutDuration = Duration(minutes: 2);

  bool get isLockedOut {
    if (lockoutUntil == null) return false;
    if (DateTime.now().isBefore(lockoutUntil!)) {
      return true;
    }
    // Lockout expired, reset
    lockoutUntil = null;
    failedAttempts = 0;
    return false;
  }

  int get remainingLockoutSeconds {
    if (lockoutUntil == null) return 0;
    final diff = lockoutUntil!.difference(DateTime.now()).inSeconds;
    return diff > 0 ? diff : 0;
  }

  MathChallenge generateTensAndFivesChallenge() {
    // Generate numbers ending in 0 or 5 (multiples of 5)
    final multiples = [5, 10, 15, 20, 25, 30, 35, 40, 45, 50];

    final type = _random.nextInt(3);

    switch (type) {
      case 0:
        // Double subtraction e.g. 10 - 5 - 5 = 0, 30 - 15 - 10 = 5
        final a = multiples[4 + _random.nextInt(6)]; // 25..50
        final b = multiples[_random.nextInt(3)]; // 5..15
        final c = multiples[_random.nextInt(2)]; // 5..10
        return MathChallenge(
          questionText: '$a - $b - $c = ?',
          correctAnswer: a - b - c,
        );

      case 1:
        // Addition then subtraction e.g. 20 + 15 - 10 = 25
        final a = multiples[_random.nextInt(5)]; // 5..25
        final b = multiples[_random.nextInt(4)]; // 5..20
        final c = multiples[_random.nextInt(3)]; // 5..15
        return MathChallenge(
          questionText: '$a + $b - $c = ?',
          correctAnswer: a + b - c,
        );

      case 2:
      default:
        // Subtraction then addition e.g. 40 - 25 + 10 = 25
        final a = multiples[3 + _random.nextInt(6)]; // 20..50
        final b = multiples[_random.nextInt(4)]; // 5..20
        final c = multiples[_random.nextInt(3)]; // 5..15
        return MathChallenge(
          questionText: '$a - $b + $c = ?',
          correctAnswer: a - b + c,
        );
    }
  }

  bool verifyAnswer(int userAnswer, int correctAnswer) {
    if (isLockedOut) return false;

    if (userAnswer == correctAnswer) {
      failedAttempts = 0;
      lockoutUntil = null;
      return true;
    } else {
      failedAttempts++;
      if (failedAttempts >= maxFailedAttempts) {
        lockoutUntil = DateTime.now().add(lockoutDuration);
      }
      return false;
    }
  }

  bool verifyPin(String enteredPin, String? expectedPin) {
    if (isLockedOut) return false;
    if (expectedPin == null || expectedPin.isEmpty) return false;

    if (enteredPin.trim() == expectedPin.trim()) {
      failedAttempts = 0;
      lockoutUntil = null;
      return true;
    } else {
      failedAttempts++;
      if (failedAttempts >= maxFailedAttempts) {
        lockoutUntil = DateTime.now().add(lockoutDuration);
      }
      return false;
    }
  }
}
