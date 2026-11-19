class StudyRecord {
  final String id;
  final DateTime timestamp;
  final int durationMinutes;
  final String mode;
  final bool wasStrict;
  final bool completedNaturally;

  const StudyRecord({
    required this.id,
    required this.timestamp,
    required this.durationMinutes,
    required this.mode,
    required this.wasStrict,
    required this.completedNaturally,
  });

  Map<String, dynamic> toJson() => {
        'id': id,
        'timestamp': timestamp.toIso8601String(),
        'durationMinutes': durationMinutes,
        'mode': mode,
        'wasStrict': wasStrict,
        'completedNaturally': completedNaturally,
      };

  factory StudyRecord.fromJson(Map<String, dynamic> json) => StudyRecord(
        id: json['id'] as String? ?? '',
        timestamp: DateTime.tryParse(json['timestamp'] as String? ?? '') ??
            DateTime.now(),
        durationMinutes: json['durationMinutes'] as int? ?? 0,
        mode: json['mode'] as String? ?? 'study',
        wasStrict: json['wasStrict'] as bool? ?? false,
        completedNaturally: json['completedNaturally'] as bool? ?? true,
      );
}
