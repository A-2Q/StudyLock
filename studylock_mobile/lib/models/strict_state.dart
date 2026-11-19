class StrictState {
  final bool isActive;
  final int remainingSeconds;
  final int totalDurationSeconds;
  final String studyUrl;
  final List<String> pdfPaths;
  final String mode;
  final String? parentPin;
  final int lastSavedEpochMs;

  const StrictState({
    required this.isActive,
    required this.remainingSeconds,
    required this.totalDurationSeconds,
    required this.studyUrl,
    this.pdfPaths = const [],
    this.mode = 'study',
    this.parentPin,
    required this.lastSavedEpochMs,
  });

  Map<String, dynamic> toJson() => {
        'isActive': isActive,
        'remainingSeconds': remainingSeconds,
        'totalDurationSeconds': totalDurationSeconds,
        'studyUrl': studyUrl,
        'pdfPaths': pdfPaths,
        'mode': mode,
        'parentPin': parentPin,
        'lastSavedEpochMs': lastSavedEpochMs,
      };

  factory StrictState.fromJson(Map<String, dynamic> json) => StrictState(
        isActive: json['isActive'] as bool? ?? false,
        remainingSeconds: json['remainingSeconds'] as int? ?? 0,
        totalDurationSeconds: json['totalDurationSeconds'] as int? ?? 0,
        studyUrl: json['studyUrl'] as String? ?? '',
        pdfPaths: (json['pdfPaths'] as List<dynamic>?)
                ?.map((e) => e.toString())
                .toList() ??
            const [],
        mode: json['mode'] as String? ?? 'study',
        parentPin: json['parentPin'] as String?,
        lastSavedEpochMs: json['lastSavedEpochMs'] as int? ?? 0,
      );
}
