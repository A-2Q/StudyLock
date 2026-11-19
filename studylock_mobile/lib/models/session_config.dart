enum SessionMode {
  study,
  child,
}

class SessionConfig {
  final String studyUrl;
  final List<String> pdfPaths;
  final int durationMinutes;
  final bool isStrictMode;
  final SessionMode mode;
  final String? parentPin;

  const SessionConfig({
    required this.studyUrl,
    this.pdfPaths = const [],
    required this.durationMinutes,
    this.isStrictMode = false,
    this.mode = SessionMode.study,
    this.parentPin,
  });

  Map<String, dynamic> toJson() => {
        'studyUrl': studyUrl,
        'pdfPaths': pdfPaths,
        'durationMinutes': durationMinutes,
        'isStrictMode': isStrictMode,
        'mode': mode.name,
        'parentPin': parentPin,
      };

  factory SessionConfig.fromJson(Map<String, dynamic> json) => SessionConfig(
        studyUrl: json['studyUrl'] as String? ?? '',
        pdfPaths: (json['pdfPaths'] as List<dynamic>?)
                ?.map((e) => e.toString())
                .toList() ??
            const [],
        durationMinutes: json['durationMinutes'] as int? ?? 25,
        isStrictMode: json['isStrictMode'] as bool? ?? false,
        mode: (json['mode'] == 'child') ? SessionMode.child : SessionMode.study,
        parentPin: json['parentPin'] as String?,
      );
}
