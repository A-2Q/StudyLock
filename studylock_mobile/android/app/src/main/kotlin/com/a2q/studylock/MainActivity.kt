package com.a2q.studylock

import android.app.ActivityManager
import android.content.Context
import android.content.Intent
import android.net.Uri
import android.os.Build
import android.os.Bundle
import android.os.PowerManager
import android.provider.Settings
import android.view.KeyEvent
import android.view.WindowManager
import androidx.annotation.NonNull
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.WindowInsetsControllerCompat
import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel

class MainActivity : FlutterActivity() {
    private val CHANNEL = "com.a2q.studylock/kiosk"
    private var isKioskActive = false
    private var isStrictMode = false
    private var wakeLock: PowerManager.WakeLock? = null

    companion object {
        var activeInstance: MainActivity? = null
        var isLocked = false
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        activeInstance = this

        if (intent.getBooleanExtra("reboot_recovery", false)) {
            // Re-arm lock from boot receiver
            isKioskActive = true
            isStrictMode = true
            isLocked = true
            enableKioskMode(true)
        }
    }

    override fun configureFlutterEngine(@NonNull flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, CHANNEL).setMethodCallHandler { call, result ->
            when (call.method) {
                "startKioskLock" -> {
                    val strict = call.argument<Boolean>("strict") ?: false
                    startKioskLock(strict)
                    result.success(true)
                }
                "stopKioskLock" -> {
                    stopKioskLock()
                    result.success(true)
                }
                "isKioskActive" -> {
                    result.success(isKioskActive)
                }
                "checkOverlayPermission" -> {
                    result.success(true)
                }
                "requestOverlayPermission" -> {
                    result.success(true)
                }
                "checkAccessibilityPermission" -> {
                    result.success(true)
                }
                "requestAccessibilityPermission" -> {
                    result.success(true)
                }
                "checkBatteryOptimization" -> {
                    result.success(checkBatteryOptimization())
                }
                "requestIgnoreBatteryOptimization" -> {
                    requestIgnoreBatteryOptimization()
                    result.success(true)
                }
                "enableKeepScreenOn" -> {
                    val enable = call.argument<Boolean>("enable") ?: true
                    setKeepScreenOn(enable)
                    result.success(true)
                }
                "bringToFront" -> {
                    bringToFront()
                    result.success(true)
                }
                else -> {
                    result.notImplemented()
                }
            }
        }
    }

    private fun startKioskLock(strict: Boolean) {
        isKioskActive = true
        isStrictMode = strict
        isLocked = true

        enableKioskMode(strict)

        // Start Foreground Watchdog Service
        try {
            val serviceIntent = Intent(this, KioskForegroundService::class.java).apply {
                putExtra("strict_mode", strict)
            }
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                startForegroundService(serviceIntent)
            } else {
                startService(serviceIntent)
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }

        // Acquire WakeLock to prevent aggressive sleep
        acquireWakeLock()
    }

    private fun stopKioskLock() {
        isKioskActive = false
        isStrictMode = false
        isLocked = false

        disableKioskMode()

        // Stop Foreground Service
        try {
            val serviceIntent = Intent(this, KioskForegroundService::class.java)
            stopService(serviceIntent)
        } catch (e: Exception) {
            e.printStackTrace()
        }

        // Release WakeLock
        releaseWakeLock()
    }

    private fun enableKioskMode(strict: Boolean) {
        runOnUiThread {
            try {
                // Apply sticky immersive mode to hide navigation bar & status bar
                val windowInsetsController = WindowCompat.getInsetsController(window, window.decorView)
                windowInsetsController.systemBarsBehavior =
                    WindowInsetsControllerCompat.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE
                windowInsetsController.hide(WindowInsetsCompat.Type.systemBars())

                window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)

                // Native Lock Task Mode (Official Google Screen Pinning / Kiosk API)
                val am = getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                    if (am.lockTaskModeState == ActivityManager.LOCK_TASK_MODE_NONE) {
                        startLockTask()
                    }
                } else {
                    startLockTask()
                }
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }

    private fun disableKioskMode() {
        runOnUiThread {
            try {
                val windowInsetsController = WindowCompat.getInsetsController(window, window.decorView)
                windowInsetsController.show(WindowInsetsCompat.Type.systemBars())
                window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)

                val am = getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                    if (am.lockTaskModeState != ActivityManager.LOCK_TASK_MODE_NONE) {
                        stopLockTask()
                    }
                } else {
                    stopLockTask()
                }
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }

    private fun acquireWakeLock() {
        try {
            if (wakeLock == null) {
                val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
                wakeLock = powerManager.newWakeLock(
                    PowerManager.PARTIAL_WAKE_LOCK,
                    "StudyLock::KioskWakeLock"
                )
                wakeLock?.acquire(12 * 60 * 60 * 1000L) // 12 hours timeout guard
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    private fun releaseWakeLock() {
        try {
            if (wakeLock != null && wakeLock?.isHeld == true) {
                wakeLock?.release()
                wakeLock = null
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    private fun setKeepScreenOn(enable: Boolean) {
        runOnUiThread {
            if (enable) {
                window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            } else {
                window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            }
        }
    }

    private fun bringToFront() {
        try {
            val intent = Intent(this, MainActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_REORDER_TO_FRONT or Intent.FLAG_ACTIVITY_SINGLE_TOP
            }
            startActivity(intent)
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    override fun onBackPressed() {
        if (isKioskActive) {
            // Strict lockdown: completely swallow back button
            return
        }
        super.onBackPressed()
    }

    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        if (isKioskActive) {
            when (keyCode) {
                KeyEvent.KEYCODE_VOLUME_UP,
                KeyEvent.KEYCODE_VOLUME_DOWN,
                KeyEvent.KEYCODE_VOLUME_MUTE -> {
                    // Allow volume controls for video lectures
                    return super.onKeyDown(keyCode, event)
                }
                KeyEvent.KEYCODE_HOME,
                KeyEvent.KEYCODE_APP_SWITCH,
                KeyEvent.KEYCODE_BACK -> {
                    // Suppress navigation hardware keys
                    return true
                }
            }
        }
        return super.onKeyDown(keyCode, event)
    }

    override fun onUserLeaveHint() {
        super.onUserLeaveHint()
        if (isKioskActive) {
            // Immediately bring activity back to foreground if user tries to switch
            bringToFront()
        }
    }

    override fun onWindowFocusChanged(hasFocus: Boolean) {
        super.onWindowFocusChanged(hasFocus)
        if (isKioskActive && !hasFocus) {
            val windowInsetsController = WindowCompat.getInsetsController(window, window.decorView)
            windowInsetsController.hide(WindowInsetsCompat.Type.systemBars())
        }
    }

    private fun checkBatteryOptimization(): Boolean {
        return try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
                powerManager.isIgnoringBatteryOptimizations(packageName)
            } else {
                true
            }
        } catch (e: Exception) {
            true
        }
    }

    private fun requestIgnoreBatteryOptimization() {
        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                val powerManager = getSystemService(Context.POWER_SERVICE) as PowerManager
                if (!powerManager.isIgnoringBatteryOptimizations(packageName)) {
                    val intent = Intent(
                        Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS,
                        Uri.parse("package:$packageName")
                    ).apply {
                        flags = Intent.FLAG_ACTIVITY_NEW_TASK
                    }
                    startActivity(intent)
                }
            }
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    override fun onDestroy() {
        if (activeInstance == this) {
            activeInstance = null
        }
        releaseWakeLock()
        super.onDestroy()
    }
}
