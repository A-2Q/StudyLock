import 'package:flutter/services.dart';

class NativeLockBridge {
  static const MethodChannel _channel = MethodChannel('com.a2q.studylock/kiosk');

  static Future<bool> startKioskLock({required bool isStrict}) async {
    try {
      final bool? result = await _channel.invokeMethod('startKioskLock', {
        'strict': isStrict,
      });
      return result ?? false;
    } on PlatformException catch (e) {
      print('Failed to start kiosk lock: ${e.message}');
      return false;
    }
  }

  static Future<bool> stopKioskLock() async {
    try {
      final bool? result = await _channel.invokeMethod('stopKioskLock');
      return result ?? false;
    } on PlatformException catch (e) {
      print('Failed to stop kiosk lock: ${e.message}');
      return false;
    }
  }

  static Future<bool> isKioskActive() async {
    try {
      final bool? result = await _channel.invokeMethod('isKioskActive');
      return result ?? false;
    } on PlatformException catch (e) {
      print('Failed to check kiosk active: ${e.message}');
      return false;
    }
  }

  static Future<bool> checkOverlayPermission() async {
    try {
      final bool? result = await _channel.invokeMethod('checkOverlayPermission');
      return result ?? false;
    } on PlatformException catch (e) {
      print('Failed to check overlay permission: ${e.message}');
      return false;
    }
  }

  static Future<void> requestOverlayPermission() async {
    try {
      await _channel.invokeMethod('requestOverlayPermission');
    } on PlatformException catch (e) {
      print('Failed to request overlay permission: ${e.message}');
    }
  }

  static Future<bool> checkAccessibilityPermission() async {
    try {
      final bool? result =
          await _channel.invokeMethod('checkAccessibilityPermission');
      return result ?? false;
    } on PlatformException catch (e) {
      print('Failed to check accessibility permission: ${e.message}');
      return false;
    }
  }

  static Future<void> requestAccessibilityPermission() async {
    try {
      await _channel.invokeMethod('requestAccessibilityPermission');
    } on PlatformException catch (e) {
      print('Failed to request accessibility permission: ${e.message}');
    }
  }

  static Future<bool> checkBatteryOptimization() async {
    try {
      final bool? result =
          await _channel.invokeMethod('checkBatteryOptimization');
      return result ?? false;
    } on PlatformException catch (e) {
      print('Failed to check battery optimization: ${e.message}');
      return false;
    }
  }

  static Future<void> requestIgnoreBatteryOptimization() async {
    try {
      await _channel.invokeMethod('requestIgnoreBatteryOptimization');
    } on PlatformException catch (e) {
      print('Failed to request battery optimization ignore: ${e.message}');
    }
  }

  static Future<void> enableKeepScreenOn(bool enable) async {
    try {
      await _channel.invokeMethod('enableKeepScreenOn', {'enable': enable});
    } on PlatformException catch (e) {
      print('Failed to set keep screen on: ${e.message}');
    }
  }

  static Future<void> bringToFront() async {
    try {
      await _channel.invokeMethod('bringToFront');
    } on PlatformException catch (e) {
      print('Failed to bring app to front: ${e.message}');
    }
  }
}
