import 'package:flutter/material.dart';

/// StudyLock Design System
/// Implements Signature OLED Charcoal & Sunrise Gold Theme with strict 4px/8px increments.
class AppTheme {
  // OLED Charcoal Palette
  static const Color bgCanvas = Color(0xFF08090B); // Deep Obsidian Base
  static const Color bgSurface1 = Color(0xFF0E1015); // Primary Sidebar / Panel
  static const Color bgSurface2 = Color(0xFF14171F); // Elevated Card / Modal
  static const Color bgSurface3 = Color(0xFF1C202B); // Hover / Interactive State
  static const Color bgGlass = Color(0xBD12141A); // 74% opacity

  // Sunrise Gold Accents
  static const Color accentPrimary = Color(0xFFF59E0B); // Sunrise Gold
  static const Color accentHover = Color(0xFFFBBF24);
  static const Color accentPressed = Color(0xFFD97706);
  static const Color accentSubtle = Color(0x1FF59E0B); // 12% opacity
  static const Color accentGlow = Color(0x47F59E0B); // 28% opacity

  // Borders & Specular Lines
  static const Color borderSubtle = Color(0x12FFFFFF); // 7% opacity
  static const Color borderAccent = Color(0x73F59E0B); // 45% opacity
  static const Color innerSpecular = Color(0x14FFFFFF); // 8% opacity

  // Typography Tokens
  static const Color textPrimary = Color(0xFFF4F5F8);
  static const Color textSecondary = Color(0xFF9CA3AF);
  static const Color textMuted = Color(0xFF646B78);

  // Semantic Tokens
  static const Color success = Color(0xFF10B981);
  static const Color warning = Color(0xFFF59E0B);
  static const Color danger = Color(0xFFEF4444);
  static const Color info = Color(0xFF3B82F6);

  // Light Theme Palette
  static const Color lightBgCanvas = Color(0xFFF8F9FA);
  static const Color lightBgSurface1 = Color(0xFFFFFFFF);
  static const Color lightBgSurface2 = Color(0xFFF1F3F5);
  static const Color lightBorderSubtle = Color(0xFFE5E7EB);
  static const Color lightTextPrimary = Color(0xFF111827);
  static const Color lightTextSecondary = Color(0xFF4B5563);

  static ThemeData get darkTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,
      scaffoldBackgroundColor: bgCanvas,
      primaryColor: accentPrimary,
      colorScheme: const ColorScheme.dark(
        primary: accentPrimary,
        secondary: accentHover,
        surface: bgSurface1,
        surfaceContainerHighest: bgSurface2,
        error: danger,
        onPrimary: Colors.black,
        onSurface: textPrimary,
      ),
      fontFamily: 'Cairo',
      cardTheme: CardTheme(
        color: bgSurface2,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: borderSubtle, width: 1),
        ),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: bgSurface1,
        elevation: 0,
        centerTitle: true,
        scrolledUnderElevation: 0,
        titleTextStyle: TextStyle(
          color: textPrimary,
          fontSize: 18,
          fontWeight: FontWeight.bold,
          fontFamily: 'Cairo',
        ),
        iconTheme: IconThemeData(color: textPrimary),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: accentPrimary,
          foregroundColor: Colors.black,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.bold,
            fontFamily: 'Cairo',
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          foregroundColor: accentPrimary,
          side: const BorderSide(color: borderAccent, width: 1.2),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            fontFamily: 'Cairo',
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: bgSurface2,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: borderSubtle),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: borderSubtle),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: accentPrimary, width: 1.5),
        ),
        hintStyle: const TextStyle(color: textMuted, fontSize: 14),
        labelStyle: const TextStyle(color: textSecondary, fontSize: 14),
      ),
      dividerTheme: const DividerThemeData(
        color: borderSubtle,
        thickness: 1,
        space: 1,
      ),
    );
  }

  static ThemeData get lightTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      scaffoldBackgroundColor: lightBgCanvas,
      primaryColor: accentPrimary,
      colorScheme: const ColorScheme.light(
        primary: accentPrimary,
        secondary: accentPressed,
        surface: lightBgSurface1,
        surfaceContainerHighest: lightBgSurface2,
        error: danger,
        onPrimary: Colors.black,
        onSurface: lightTextPrimary,
      ),
      fontFamily: 'Cairo',
      cardTheme: CardTheme(
        color: lightBgSurface1,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
          side: const BorderSide(color: lightBorderSubtle, width: 1),
        ),
      ),
      appBarTheme: const AppBarTheme(
        backgroundColor: lightBgSurface1,
        elevation: 0,
        centerTitle: true,
        scrolledUnderElevation: 0,
        titleTextStyle: TextStyle(
          color: lightTextPrimary,
          fontSize: 18,
          fontWeight: FontWeight.bold,
          fontFamily: 'Cairo',
        ),
        iconTheme: IconThemeData(color: lightTextPrimary),
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: accentPrimary,
          foregroundColor: Colors.black,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.bold,
            fontFamily: 'Cairo',
          ),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: lightBgSurface2,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: lightBorderSubtle),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: lightBorderSubtle),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: accentPrimary, width: 1.5),
        ),
        hintStyle: const TextStyle(color: textMuted, fontSize: 14),
        labelStyle: const TextStyle(color: lightTextSecondary, fontSize: 14),
      ),
    );
  }
}
