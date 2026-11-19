import 'dart:math';
import 'package:flutter/material.dart';
import '../../core/localization/app_localizations.dart';
import '../../core/theme/app_theme.dart';

class EmergencyExitDialog extends StatefulWidget {
  final VoidCallback onEmergencyExitConfirmed;

  const EmergencyExitDialog({
    super.key,
    required this.onEmergencyExitConfirmed,
  });

  @override
  State<EmergencyExitDialog> createState() => _EmergencyExitDialogState();
}

class _EmergencyExitDialogState extends State<EmergencyExitDialog> {
  late final String _targetEmergencyCode;
  final TextEditingController _inputController = TextEditingController();
  bool _isMatch = false;

  @override
  void initState() {
    super.initState();
    _targetEmergencyCode = _generateEmergencyCode(55);
    _inputController.addListener(_checkMatch);
  }

  String _generateEmergencyCode(int length) {
    const chars =
        'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#%&*';
    final random = Random.secure();
    return List.generate(length, (_) => chars[random.nextInt(chars.length)]).join();
  }

  void _checkMatch() {
    setState(() {
      _isMatch = (_inputController.text.trim() == _targetEmergencyCode);
    });
  }

  @override
  void dispose() {
    _inputController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return AlertDialog(
      backgroundColor: AppTheme.bgSurface2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: AppTheme.danger, width: 1.2),
      ),
      title: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppTheme.danger.withOpacity(0.15),
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Icon(Icons.warning_amber_rounded, color: AppTheme.danger, size: 24),
          ),
          const SizedBox(width: 12),
          Text(
            l10n.translate('emergency_exit_title'),
            style: const TextStyle(color: AppTheme.danger, fontSize: 18, fontWeight: FontWeight.bold),
          ),
        ],
      ),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              l10n.translate('emergency_exit_prompt'),
              style: const TextStyle(color: AppTheme.textSecondary, fontSize: 13),
            ),
            const SizedBox(height: 16),

            // 55-char Target Display Box
            Container(
              padding: const EdgeInsets.all(14),
              decoration: BoxDecoration(
                color: AppTheme.bgCanvas,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppTheme.borderSubtle),
              ),
              child: SelectableText(
                _targetEmergencyCode,
                style: const TextStyle(
                  color: AppTheme.accentPrimary,
                  fontSize: 13,
                  fontFamily: 'monospace',
                  letterSpacing: 1.2,
                  height: 1.5,
                ),
              ),
            ),
            const SizedBox(height: 16),

            // Input TextField with Paste Disabled
            TextField(
              controller: _inputController,
              enableInteractiveSelection: false, // Disables copy/cut/paste popup menu!
              maxLines: 2,
              style: const TextStyle(
                color: AppTheme.textPrimary,
                fontSize: 13,
                fontFamily: 'monospace',
              ),
              decoration: InputDecoration(
                hintText: 'اكتب الرمز هنا حرفاً بحرف...',
                helperText: 'المدخل: ${_inputController.text.length} / ${_targetEmergencyCode.length}',
                helperStyle: TextStyle(
                  color: _isMatch ? AppTheme.success : AppTheme.textMuted,
                ),
                suffixIcon: _isMatch
                    ? const Icon(Icons.check_circle_rounded, color: AppTheme.success)
                    : null,
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('إلغاء والعودة للمذاكرة', style: TextStyle(color: AppTheme.textSecondary)),
        ),
        ElevatedButton(
          onPressed: _isMatch
              ? () {
                  Navigator.of(context).pop();
                  widget.onEmergencyExitConfirmed();
                }
              : null,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppTheme.danger,
            foregroundColor: Colors.white,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
          child: Text(
            l10n.translate('emergency_exit_confirm'),
            style: const TextStyle(fontWeight: FontWeight.bold),
          ),
        ),
      ],
    );
  }
}
