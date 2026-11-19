package com.a2q.studylock

import android.content.Context
import android.graphics.PixelFormat
import android.os.Build
import android.view.Gravity
import android.view.MotionEvent
import android.view.View
import android.view.WindowManager

class KioskLockManager(private val context: Context) {
    private val windowManager = context.getSystemService(Context.WINDOW_SERVICE) as WindowManager
    private var shieldView: View? = null
    private var isShieldAttached = false

    fun attachNotificationShield() {
        if (isShieldAttached) return

        try {
            val params = WindowManager.LayoutParams().apply {
                type = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                    WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY
                } else {
                    @Suppress("DEPRECATION")
                    WindowManager.LayoutParams.TYPE_SYSTEM_ERROR
                }
                format = PixelFormat.TRANSLUCENT
                flags = (WindowManager.LayoutParams.FLAG_NOT_FOCUSABLE
                        or WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN
                        or WindowManager.LayoutParams.FLAG_LAYOUT_NO_LIMITS)
                width = WindowManager.LayoutParams.MATCH_PARENT
                // Shield top 40dp area to intercept swipe down before system shade catches it
                height = (40 * context.resources.displayMetrics.density).toInt()
                gravity = Gravity.TOP or Gravity.START
            }

            shieldView = View(context).apply {
                setBackgroundColor(0x01000000) // Almost completely invisible but captures touches
                setOnTouchListener { _, event ->
                    when (event.action) {
                        MotionEvent.ACTION_DOWN,
                        MotionEvent.ACTION_MOVE -> {
                            // Swallow touch event to prevent notification shade drag
                            true
                        }
                        else -> true
                    }
                }
            }

            windowManager.addView(shieldView, params)
            isShieldAttached = true
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    fun detachNotificationShield() {
        if (!isShieldAttached || shieldView == null) return

        try {
            windowManager.removeView(shieldView)
            shieldView = null
            isShieldAttached = false
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }
}
