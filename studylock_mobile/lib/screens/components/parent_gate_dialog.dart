import 'package:flutter/material.dart';
import '../../core/localization/app_localizations.dart';
import '../../core/theme/app_theme.dart';
import '../../services/math_gate_service.dart';

class ParentGateDialog extends StatefulWidget {
  final MathGateService gateService;
  final String? parentPin;
  final VoidCallback onUnlockSuccess;

  const ParentGateDialog({
    super.key,
    required this.gateService,
    this.parentPin,
    required this.onUnlockSuccess,
  });

  @override
  State<ParentGateDialog> createState() => _ParentGateDialogState();
}

class _ParentGateDialogState extends State<ParentGateDialog> {
  late MathChallenge _currentChallenge;
  final TextEditingController _answerController = TextEditingController();
  final TextEditingController _pinController = TextEditingController();
  bool _usePinMode = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _currentChallenge = widget.gateService.generateTensAndFivesChallenge();
  }

  void _verifyChallenge() {
    if (widget.gateService.isLockedOut) {
      setState(() {
        _errorMessage =
            '${AppLocalizations.of(context).translate('parent_gate_locked')} (${widget.gateService.remainingLockoutSeconds}s)';
      });
      return;
    }

    if (_usePinMode) {
      final isPinValid = widget.gateService.verifyPin(
        _pinController.text,
        widget.parentPin,
      );
      if (isPinValid) {
        Navigator.of(context).pop();
        widget.onUnlockSuccess();
      } else {
        setState(() {
          _errorMessage = AppLocalizations.of(context).translate('parent_gate_wrong');
        });
      }
    } else {
      final userAnswer = int.tryParse(_answerController.text.trim());
      if (userAnswer == null) {
        setState(() {
          _errorMessage = AppLocalizations.of(context).translate('parent_gate_wrong');
        });
        return;
      }

      final isSuccess = widget.gateService.verifyAnswer(
        userAnswer,
        _currentChallenge.correctAnswer,
      );

      if (isSuccess) {
        Navigator.of(context).pop();
        widget.onUnlockSuccess();
      } else {
        setState(() {
          _errorMessage = AppLocalizations.of(context).translate('parent_gate_wrong');
          // Regenerate challenge on error
          _currentChallenge = widget.gateService.generateTensAndFivesChallenge();
          _answerController.clear();
        });
      }
    }
  }

  @override
  void dispose() {
    _answerController.dispose();
    _pinController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final isLockedOut = widget.gateService.isLockedOut;

    return AlertDialog(
      backgroundColor: AppTheme.bgSurface2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: const BorderSide(color: AppTheme.borderSubtle),
      ),
      title: Row(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppTheme.accentSubtle,
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Icon(Icons.security_rounded, color: AppTheme.accentPrimary, size: 22),
          ),
          const SizedBox(width: 12),
          Text(
            l10n.translate('parent_gate_title'),
            style: const TextStyle(color: AppTheme.textPrimary, fontSize: 18),
          ),
        ],
      ),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (isLockedOut) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppTheme.danger.withOpacity(0.15),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AppTheme.danger.withOpacity(0.4)),
                ),
                child: Text(
                  '${l10n.translate('parent_gate_locked')} (${widget.gateService.remainingLockoutSeconds}s)',
                  style: const TextStyle(color: AppTheme.danger, fontSize: 13),
                  textAlign: TextAlign.center,
                ),
              ),
              const SizedBox(height: 16),
            ] else if (!_usePinMode) ...[
              Text(
                l10n.translate('parent_gate_prompt'),
                style: const TextStyle(color: AppTheme.textSecondary, fontSize: 14),
              ),
              const SizedBox(height: 16),
              // Arithmetic Math Box
              Container(
                padding: const EdgeInsets.symmetric(vertical: 18),
                decoration: BoxDecoration(
                  color: AppTheme.bgSurface1,
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: AppTheme.borderAccent),
                ),
                child: Center(
                  child: Text(
                    _currentChallenge.questionText,
                    style: const TextStyle(
                      color: AppTheme.accentPrimary,
                      fontSize: 26,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 2,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _answerController,
                keyboardType: TextInputType.number,
                autofocus: true,
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppTheme.textPrimary, fontSize: 20, fontWeight: FontWeight.bold),
                decoration: InputDecoration(
                  hintText: '؟',
                  errorText: _errorMessage,
                ),
                onSubmitted: (_) => _verifyChallenge(),
              ),
            ] else ...[
              Text(
                l10n.translate('parent_pin_label'),
                style: const TextStyle(color: AppTheme.textSecondary, fontSize: 14),
              ),
              const SizedBox(height: 16),
              TextField(
                controller: _pinController,
                keyboardType: TextInputType.number,
                obscureText: true,
                autofocus: true,
                textAlign: TextAlign.center,
                maxLength: 6,
                style: const TextStyle(color: AppTheme.textPrimary, fontSize: 22, letterSpacing: 8),
                decoration: InputDecoration(
                  hintText: '••••',
                  counterText: '',
                  errorText: _errorMessage,
                ),
                onSubmitted: (_) => _verifyChallenge(),
              ),
            ],
            const SizedBox(height: 12),

            // Toggle PIN / Challenge if PIN is configured
            if (widget.parentPin != null && widget.parentPin!.isNotEmpty)
              TextButton(
                onPressed: () {
                  setState(() {
                    _usePinMode = !_usePinMode;
                    _errorMessage = null;
                  });
                },
                child: Text(
                  _usePinMode
                      ? l10n.translate('parent_gate_title')
                      : l10n.translate('use_parent_pin'),
                  style: const TextStyle(color: AppTheme.textSecondary, fontSize: 13),
                ),
              ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('إلغاء', style: TextStyle(color: AppTheme.textMuted)),
        ),
        ElevatedButton(
          onPressed: isLockedOut ? null : _verifyChallenge,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppTheme.accentPrimary,
            foregroundColor: Colors.black,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
          child: Text(
            l10n.translate('parent_gate_submit'),
            style: const TextStyle(fontWeight: FontWeight.bold),
          ),
        ),
      ],
    );
  }
}
