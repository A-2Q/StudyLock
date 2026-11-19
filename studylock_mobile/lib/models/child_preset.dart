import 'package:flutter/material.dart';

class ChildPreset {
  final String id;
  final String titleAr;
  final String titleEn;
  final String url;
  final IconData icon;
  final Color themeColor;

  const ChildPreset({
    required this.id,
    required this.titleAr,
    required this.titleEn,
    required this.url,
    required this.icon,
    required this.themeColor,
  });

  static const List<ChildPreset> defaultPresets = [
    ChildPreset(
      id: 'khan_kids',
      titleAr: 'خان أكاديمي للأطفال',
      titleEn: 'Khan Academy Kids',
      url: 'https://learn.khanacademy.org/khan-academy-kids/',
      icon: Icons.school_rounded,
      themeColor: Color(0xFF14BF96),
    ),
    ChildPreset(
      id: 'duolingo',
      titleAr: 'دوولينجو لتعلم اللغات',
      titleEn: 'Duolingo',
      url: 'https://www.duolingo.com',
      icon: Icons.language_rounded,
      themeColor: Color(0xFF58CC02),
    ),
    ChildPreset(
      id: 'scratch',
      titleAr: 'برمجة سكراتش',
      titleEn: 'Scratch Studio',
      url: 'https://scratch.mit.edu',
      icon: Icons.code_rounded,
      themeColor: Color(0xFFFFAB19),
    ),
    ChildPreset(
      id: 'madrasati',
      titleAr: 'منصة مدرستي',
      titleEn: 'Madrasati Portal',
      url: 'https://schools.madrasati.sa',
      icon: Icons.menu_book_rounded,
      themeColor: Color(0xFF007A87),
    ),
    ChildPreset(
      id: 'nafham',
      titleAr: 'نفهم - دروس تفاعلية',
      titleEn: 'Nafham Education',
      url: 'https://www.nafham.com',
      icon: Icons.play_lesson_rounded,
      themeColor: Color(0xFFE53935),
    ),
    ChildPreset(
      id: 'natgeo_kids',
      titleAr: 'ناشيونال جيوغرافيك كيدز',
      titleEn: 'Nat Geo Kids',
      url: 'https://kids.nationalgeographic.com',
      icon: Icons.public_rounded,
      themeColor: Color(0xFFFFD100),
    ),
  ];
}
