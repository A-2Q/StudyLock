package com.a2q.studylock

import android.accessibilityservice.AccessibilityService
import android.content.Intent
import android.view.accessibility.AccessibilityEvent

class KioskAccessibilityService : AccessibilityService() {

    override fun onAccessibilityEvent(event: AccessibilityEvent?) {
        if (!MainActivity.isLocked) return

        if (event?.eventType == AccessibilityEvent.TYPE_WINDOW_STATE_CHANGED) {
            val packageName = event.packageName?.toString() ?: ""
            val className = event.className?.toString() ?: ""

            // Check if user is navigating into system settings or system UI panels
            val isSystemSettings = packageName.contains("settings", ignoreCase = true)
            val isRecentApps = className.contains("recents", ignoreCase = true)
            val isStatusBarWindow = className.contains("StatusBarWindowView", ignoreCase = true) ||
                    className.contains("NotificationPanelView", ignoreCase = true)

            if (isSystemSettings || isRecentApps || isStatusBarWindow) {
                // Collapse system status bar and windows
                performGlobalAction(GLOBAL_ACTION_BACK)
                performGlobalAction(GLOBAL_ACTION_HOME)

                val closeDialogs = Intent(Intent.ACTION_CLOSE_SYSTEM_DIALOGS)
                sendBroadcast(closeDialogs)

                // Relaunch StudyLock to front
                MainActivity.activeInstance?.runOnUiThread {
                    val intent = Intent(this, MainActivity::class.java).apply {
                        flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_REORDER_TO_FRONT
                    }
                    startActivity(intent)
                }
            }
        }
    }

    override fun onInterrupt() {
        // Required callback
    }
}
