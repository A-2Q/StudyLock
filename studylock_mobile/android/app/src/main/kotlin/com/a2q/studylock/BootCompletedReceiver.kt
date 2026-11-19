package com.a2q.studylock

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.SharedPreferences

class BootCompletedReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        val action = intent.action
        if (action == Intent.ACTION_BOOT_COMPLETED ||
            action == "android.intent.action.QUICKBOOT_POWERON" ||
            action == "com.htc.intent.action.QUICKBOOT_POWERON") {

            val prefs: SharedPreferences = context.getSharedPreferences("FlutterSharedPreferences", Context.MODE_PRIVATE)
            val isStrictActive = prefs.getBoolean("flutter.strict_session_active", false)
            val remainingSeconds = prefs.getInt("flutter.session_remaining_seconds", 0)

            if (isStrictActive && remainingSeconds > 0) {
                // Device rebooted during an active strict session: re-launch and enforce lock
                val launchIntent = Intent(context, MainActivity::class.java).apply {
                    flags = Intent.FLAG_ACTIVITY_NEW_TASK or
                            Intent.FLAG_ACTIVITY_CLEAR_TOP or
                            Intent.FLAG_ACTIVITY_SINGLE_TOP
                    putExtra("reboot_recovery", true)
                }
                context.startActivity(launchIntent)
            }
        }
    }
}
