import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../core/localization/app_localizations.dart';
import '../../core/native/native_lock_bridge.dart';
import '../../core/theme/app_theme.dart';
import '../../models/child_preset.dart';
import '../../models/session_config.dart';
import '../../models/strict_state.dart';
import '../../models/study_record.dart';
import '../../services/storage_service.dart';
import 'child_lock_screen.dart';
import 'study_lock_screen.dart';

class HomeSetupScreen extends StatefulWidget {
  final StorageService storageService;
  final Function(Locale) onLocaleChanged;
  final Function(bool) onThemeModeChanged;

  const HomeSetupScreen({
    super.key,
    required this.storageService,
    required this.onLocaleChanged,
    required this.onThemeModeChanged,
  });

  @override
  State<HomeSetupScreen> createState() => _HomeSetupScreenState();
}

class _HomeSetupScreenState extends State<HomeSetupScreen> {
  int _currentTabIndex = 0;

  // Study Setup Form State
  final TextEditingController _studyUrlController =
      TextEditingController(text: 'https://www.khanacademy.org');
  final List<String> _selectedPdfPaths = [];
  int _studyDurationMinutes = 25;
  bool _isStrictMode = false;

  // Child Setup State
  ChildPreset _selectedChildPreset = ChildPreset.defaultPresets.first;
  final TextEditingController _childCustomUrlController = TextEditingController();
  int _childDurationMinutes = 30;
  final TextEditingController _parentPinController = TextEditingController();

  // Permissions State
  bool _hasOverlayPermission = false;
  bool _hasAccessibilityPermission = false;
  bool _hasBatteryOptimization = false;

  @override
  void initState() {
    super.initState();
    _refreshPermissions();
    _parentPinController.text = widget.storageService.getParentPin() ?? '';
  }

  Future<void> _refreshPermissions() async {
    final overlay = await NativeLockBridge.checkOverlayPermission();
    final access = await NativeLockBridge.checkAccessibilityPermission();
    final battery = await NativeLockBridge.checkBatteryOptimization();

    if (mounted) {
      setState(() {
        _hasOverlayPermission = overlay;
        _hasAccessibilityPermission = access;
        _hasBatteryOptimization = battery;
      });
    }
  }

  Future<void> _pickPdfs() async {
    try {
      final result = await FilePicker.platform.pickFiles(
        type: FileType.custom,
        allowedExtensions: ['pdf'],
        allowMultiple: true,
      );

      if (result != null && result.paths.isNotEmpty) {
        setState(() {
          for (final path in result.paths) {
            if (path != null && !_selectedPdfPaths.contains(path)) {
              _selectedPdfPaths.add(path);
            }
          }
        });
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('فشل في اختيار ملفات PDF: $e')),
      );
    }
  }

  void _startStudySession() async {
    final rawUrl = _studyUrlController.text.trim();
    if (rawUrl.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(AppLocalizations.of(context).translate('study_url_required')),
          backgroundColor: AppTheme.danger,
        ),
      );
      return;
    }

    final config = SessionConfig(
      studyUrl: rawUrl,
      pdfPaths: _selectedPdfPaths,
      durationMinutes: _studyDurationMinutes,
      isStrictMode: _isStrictMode,
      mode: SessionMode.study,
    );

    if (_isStrictMode) {
      final strictState = StrictState(
        isActive: true,
        remainingSeconds: _studyDurationMinutes * 60,
        totalDurationSeconds: _studyDurationMinutes * 60,
        studyUrl: rawUrl,
        pdfPaths: _selectedPdfPaths,
        mode: 'study',
        lastSavedEpochMs: DateTime.now().millisecondsSinceEpoch,
      );
      await widget.storageService.saveStrictState(strictState);
    }

    if (!mounted) return;
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (context) => StudyLockScreen(
          config: config,
          storageService: widget.storageService,
        ),
      ),
    );

    setState(() {}); // refresh stats after returning
  }

  void _startChildSession() async {
    final url = _childCustomUrlController.text.trim().isNotEmpty
        ? _childCustomUrlController.text.trim()
        : _selectedChildPreset.url;

    final pin = _parentPinController.text.trim();
    if (pin.isNotEmpty) {
      await widget.storageService.setParentPin(pin);
    }

    final config = SessionConfig(
      studyUrl: url,
      durationMinutes: _childDurationMinutes,
      isStrictMode: true,
      mode: SessionMode.child,
      parentPin: pin.isNotEmpty ? pin : null,
    );

    if (!mounted) return;
    await Navigator.of(context).push(
      MaterialPageRoute(
        builder: (context) => ChildLockScreen(
          config: config,
          storageService: widget.storageService,
        ),
      ),
    );

    setState(() {}); // refresh stats
  }

  @override
  void dispose() {
    _studyUrlController.dispose();
    _childCustomUrlController.dispose();
    _parentPinController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      backgroundColor: AppTheme.bgCanvas,
      appBar: AppBar(
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Image.asset(
              'assets/icons/app_logo.png',
              width: 28,
              height: 28,
              errorBuilder: (_, __, ___) => const Icon(
                Icons.lock_clock_rounded,
                color: AppTheme.accentPrimary,
              ),
            ),
            const SizedBox(width: 10),
            Text(l10n.translate('app_title')),
          ],
        ),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 820),
          child: IndexedStack(
            index: _currentTabIndex,
            children: [
              _buildStudyTab(l10n),
              _buildChildTab(l10n),
              _buildAnalyticsTab(l10n),
              _buildSettingsTab(l10n),
            ],
          ),
        ),
      ),
      bottomNavigationBar: Container(
        decoration: const BoxDecoration(
          color: AppTheme.bgSurface1,
          border: Border(top: BorderSide(color: AppTheme.borderSubtle)),
        ),
        child: NavigationBar(
          selectedIndex: _currentTabIndex,
          onDestinationSelected: (index) {
            setState(() {
              _currentTabIndex = index;
            });
            if (index == 3) {
              _refreshPermissions();
            }
          },
          backgroundColor: AppTheme.bgSurface1,
          indicatorColor: AppTheme.accentSubtle,
          destinations: [
            NavigationDestination(
              icon: const Icon(Icons.school_outlined),
              selectedIcon: const Icon(Icons.school_rounded, color: AppTheme.accentPrimary),
              label: l10n.translate('tab_study'),
            ),
            NavigationDestination(
              icon: const Icon(Icons.child_care_outlined),
              selectedIcon: const Icon(Icons.child_care_rounded, color: AppTheme.accentPrimary),
              label: l10n.translate('tab_child'),
            ),
            NavigationDestination(
              icon: const Icon(Icons.bar_chart_outlined),
              selectedIcon: const Icon(Icons.bar_chart_rounded, color: AppTheme.accentPrimary),
              label: l10n.translate('tab_analytics'),
            ),
            NavigationDestination(
              icon: const Icon(Icons.settings_outlined),
              selectedIcon: const Icon(Icons.settings_rounded, color: AppTheme.accentPrimary),
              label: l10n.translate('tab_settings'),
            ),
          ],
        ),
      ),
    );
  }

  // TAB 1: Study Mode Setup
  Widget _buildStudyTab(AppLocalizations l10n) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Header Card
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppTheme.bgSurface2,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppTheme.borderSubtle),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: AppTheme.accentSubtle,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(Icons.lock_person_rounded, color: AppTheme.accentPrimary, size: 28),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        l10n.translate('app_title'),
                        style: const TextStyle(
                          color: AppTheme.textPrimary,
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Text(
                        l10n.translate('tagline'),
                        style: const TextStyle(color: AppTheme.textSecondary, fontSize: 12),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),

          // URL Input
          Text(
            l10n.translate('study_url_label'),
            style: const TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          TextField(
            controller: _studyUrlController,
            style: const TextStyle(color: AppTheme.textPrimary),
            decoration: InputDecoration(
              hintText: l10n.translate('study_url_hint'),
              prefixIcon: const Icon(Icons.link_rounded, color: AppTheme.accentPrimary),
              suffixIcon: IconButton(
                icon: const Icon(Icons.clear_rounded, size: 18),
                onPressed: () => _studyUrlController.clear(),
              ),
            ),
          ),
          const SizedBox(height: 20),

          // Attached PDFs
          Text(
            l10n.translate('pdf_select_label'),
            style: const TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            onPressed: _pickPdfs,
            icon: const Icon(Icons.picture_as_pdf_rounded),
            label: Text(l10n.translate('pdf_select_button')),
          ),
          if (_selectedPdfPaths.isNotEmpty) ...[
            const SizedBox(height: 10),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: _selectedPdfPaths.map((path) {
                final name = path.split(RegExp(r'[\\/]')).last;
                return Chip(
                  backgroundColor: AppTheme.bgSurface3,
                  label: Text(name, style: const TextStyle(color: AppTheme.textPrimary, fontSize: 12)),
                  deleteIcon: const Icon(Icons.close, size: 16),
                  onDeleted: () {
                    setState(() {
                      _selectedPdfPaths.remove(path);
                    });
                  },
                );
              }).toList(),
            ),
          ],
          const SizedBox(height: 24),

          // Duration Slider & Quick Presets
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                l10n.translate('duration_label'),
                style: const TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                decoration: BoxDecoration(
                  color: AppTheme.accentSubtle,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  '$_studyDurationMinutes ${l10n.translate('minutes_short')}',
                  style: const TextStyle(
                    color: AppTheme.accentPrimary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
          Slider(
            value: _studyDurationMinutes.toDouble(),
            min: 5,
            max: 300,
            divisions: 59,
            activeColor: AppTheme.accentPrimary,
            inactiveColor: AppTheme.bgSurface3,
            onChanged: (val) {
              setState(() {
                _studyDurationMinutes = val.round();
              });
            },
          ),
          Wrap(
            spacing: 8,
            children: [15, 25, 45, 60, 90, 120].map((preset) {
              final isSelected = _studyDurationMinutes == preset;
              return ChoiceChip(
                label: Text('$preset د'),
                selected: isSelected,
                selectedColor: AppTheme.accentPrimary,
                labelStyle: TextStyle(
                  color: isSelected ? Colors.black : AppTheme.textSecondary,
                  fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                ),
                onSelected: (_) {
                  setState(() {
                    _studyDurationMinutes = preset;
                  });
                },
              );
            }).toList(),
          ),
          const SizedBox(height: 24),

          // Strict Mode Toggle Card
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: _isStrictMode
                  ? AppTheme.danger.withOpacity(0.08)
                  : AppTheme.bgSurface2,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(
                color: _isStrictMode ? AppTheme.danger : AppTheme.borderSubtle,
              ),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Row(
                      children: [
                        Icon(
                          Icons.gavel_rounded,
                          color: _isStrictMode ? AppTheme.danger : AppTheme.textSecondary,
                        ),
                        const SizedBox(width: 10),
                        Text(
                          l10n.translate('strict_mode_title'),
                          style: TextStyle(
                            color: _isStrictMode ? AppTheme.danger : AppTheme.textPrimary,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                    Switch(
                      value: _isStrictMode,
                      activeColor: AppTheme.danger,
                      onChanged: (val) {
                        setState(() {
                          _isStrictMode = val;
                        });
                      },
                    ),
                  ],
                ),
                const SizedBox(height: 6),
                Text(
                  _isStrictMode
                      ? l10n.translate('strict_mode_desc')
                      : l10n.translate('standard_mode_desc'),
                  style: const TextStyle(color: AppTheme.textSecondary, fontSize: 12),
                ),
              ],
            ),
          ),
          const SizedBox(height: 28),

          // Start Button
          ElevatedButton.icon(
            onPressed: _startStudySession,
            icon: const Icon(Icons.lock_rounded),
            label: Text(l10n.translate('start_lock_button')),
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16),
            ),
          ),
        ],
      ),
    );
  }

  // TAB 2: Child Safe Mode
  Widget _buildChildTab(AppLocalizations l10n) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Banner
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppTheme.bgSurface2,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppTheme.borderSubtle),
            ),
            child: Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: const Color(0xFF14BF96).withOpacity(0.15),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(Icons.child_care_rounded, color: Color(0xFF14BF96), size: 28),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        l10n.translate('child_mode_title'),
                        style: const TextStyle(
                          color: AppTheme.textPrimary,
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Text(
                        l10n.translate('child_mode_desc'),
                        style: const TextStyle(color: AppTheme.textSecondary, fontSize: 12),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),

          // Presets Grid
          Text(
            l10n.translate('child_preset_select'),
            style: const TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 12),
          GridView.builder(
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: MediaQuery.of(context).size.width >= 600 ? 3 : 2,
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.6,
            ),
            itemCount: ChildPreset.defaultPresets.length,
            itemBuilder: (context, index) {
              final preset = ChildPreset.defaultPresets[index];
              final isSelected = _selectedChildPreset.id == preset.id;

              return GestureDetector(
                onTap: () {
                  setState(() {
                    _selectedChildPreset = preset;
                    _childCustomUrlController.clear();
                  });
                },
                child: Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: isSelected ? AppTheme.bgSurface3 : AppTheme.bgSurface2,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(
                      color: isSelected ? AppTheme.accentPrimary : AppTheme.borderSubtle,
                      width: isSelected ? 2 : 1,
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Icon(preset.icon, color: preset.themeColor, size: 28),
                      Text(
                        preset.titleAr,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(
                          color: isSelected ? AppTheme.accentPrimary : AppTheme.textPrimary,
                          fontWeight: FontWeight.bold,
                          fontSize: 13,
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
          const SizedBox(height: 20),

          // Duration Slider
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                l10n.translate('duration_label'),
                style: const TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
              ),
              Text(
                '$_childDurationMinutes ${l10n.translate('minutes_short')}',
                style: const TextStyle(
                  color: AppTheme.accentPrimary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ],
          ),
          Slider(
            value: _childDurationMinutes.toDouble(),
            min: 10,
            max: 180,
            divisions: 17,
            activeColor: AppTheme.accentPrimary,
            inactiveColor: AppTheme.bgSurface3,
            onChanged: (val) {
              setState(() {
                _childDurationMinutes = val.round();
              });
            },
          ),
          const SizedBox(height: 16),

          // Parent Security PIN
          Text(
            l10n.translate('parent_pin_label'),
            style: const TextStyle(color: AppTheme.textPrimary, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          TextField(
            controller: _parentPinController,
            keyboardType: TextInputType.number,
            obscureText: true,
            maxLength: 6,
            style: const TextStyle(color: AppTheme.textPrimary),
            decoration: InputDecoration(
              hintText: l10n.translate('parent_pin_hint'),
              prefixIcon: const Icon(Icons.pin_rounded, color: AppTheme.accentPrimary),
              counterText: '',
            ),
          ),
          const SizedBox(height: 28),

          // Start Child Lock Button
          ElevatedButton.icon(
            onPressed: _startChildSession,
            icon: const Icon(Icons.shield_rounded),
            label: Text(l10n.translate('start_child_lock')),
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16),
            ),
          ),
        ],
      ),
    );
  }

  // TAB 3: Analytics
  Widget _buildAnalyticsTab(AppLocalizations l10n) {
    final records = widget.storageService.getStudyRecords();
    final totalMinutes = records.fold<int>(0, (sum, r) => sum + r.durationMinutes);
    final totalHours = (totalMinutes / 60.0).toStringAsFixed(1);
    final completedCount = records.where((r) => r.completedNaturally).length;
    final strictRatio = records.isEmpty
        ? '0%'
        : '${((records.where((r) => r.wasStrict).length / records.length) * 100).round()}%';

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Stat Cards Row
          Row(
            children: [
              Expanded(
                child: _buildMetricCard(
                  title: l10n.translate('analytics_total_hours'),
                  value: totalHours,
                  icon: Icons.hourglass_top_rounded,
                  color: AppTheme.accentPrimary,
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: _buildMetricCard(
                  title: l10n.translate('analytics_sessions_count'),
                  value: '$completedCount',
                  icon: Icons.task_alt_rounded,
                  color: AppTheme.success,
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),
          _buildMetricCard(
            title: l10n.translate('analytics_strict_ratio'),
            value: strictRatio,
            icon: Icons.verified_user_rounded,
            color: AppTheme.info,
          ),
          const SizedBox(height: 24),

          // History List
          const Text(
            'سجل الجلسات السابقة',
            style: TextStyle(
              color: AppTheme.textPrimary,
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          if (records.isEmpty)
            Container(
              padding: const EdgeInsets.all(32),
              alignment: Alignment.center,
              child: Text(
                l10n.translate('analytics_no_data'),
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppTheme.textMuted),
              ),
            )
          else
            ListView.separated(
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              itemCount: records.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, index) {
                final r = records[index];
                return Container(
                  padding: const EdgeInsets.all(14),
                  decoration: BoxDecoration(
                    color: AppTheme.bgSurface2,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: AppTheme.borderSubtle),
                  ),
                  child: Row(
                    children: [
                      Icon(
                        r.completedNaturally ? Icons.check_circle : Icons.cancel_outlined,
                        color: r.completedNaturally ? AppTheme.success : AppTheme.danger,
                        size: 20,
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              r.mode == 'child' ? 'جلسة طفل' : 'جلسة مذاكرة',
                              style: const TextStyle(
                                color: AppTheme.textPrimary,
                                fontWeight: FontWeight.bold,
                                fontSize: 14,
                              ),
                            ),
                            Text(
                              '${r.timestamp.year}/${r.timestamp.month}/${r.timestamp.day} - ${r.timestamp.hour}:${r.timestamp.minute.toString().padLeft(2, '0')}',
                              style: const TextStyle(color: AppTheme.textMuted, fontSize: 11),
                            ),
                          ],
                        ),
                      ),
                      Text(
                        '${r.durationMinutes} د',
                        style: const TextStyle(
                          color: AppTheme.accentPrimary,
                          fontWeight: FontWeight.bold,
                          fontSize: 14,
                        ),
                      ),
                    ],
                  ),
                );
              },
            ),
        ],
      ),
    );
  }

  Widget _buildMetricCard({
    required String title,
    required String value,
    required IconData icon,
    required Color color,
  }) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppTheme.bgSurface2,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppTheme.borderSubtle),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: color, size: 24),
          const SizedBox(height: 12),
          Text(
            value,
            style: TextStyle(
              color: color,
              fontSize: 24,
              fontWeight: FontWeight.bold,
              fontFamily: 'monospace',
            ),
          ),
          const SizedBox(height: 4),
          Text(title, style: const TextStyle(color: AppTheme.textSecondary, fontSize: 12)),
        ],
      ),
    );
  }

  // TAB 4: Settings & System Permissions
  Widget _buildSettingsTab(AppLocalizations l10n) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          // Security Permissions Section
          const Text(
            'أذونات الحماية والقفل في النظام',
            style: TextStyle(
              color: AppTheme.textPrimary,
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),

          // Overlay Permission Card
          _buildPermissionTile(
            title: l10n.translate('permission_overlay_title'),
            desc: l10n.translate('permission_overlay_desc'),
            isGranted: _hasOverlayPermission,
            onTap: () async {
              await NativeLockBridge.requestOverlayPermission();
              await _refreshPermissions();
            },
          ),
          const SizedBox(height: 8),

          // Accessibility Card
          _buildPermissionTile(
            title: l10n.translate('permission_access_title'),
            desc: l10n.translate('permission_access_desc'),
            isGranted: _hasAccessibilityPermission,
            onTap: () async {
              await NativeLockBridge.requestAccessibilityPermission();
              await _refreshPermissions();
            },
          ),
          const SizedBox(height: 8),

          // Battery Optimization Card
          _buildPermissionTile(
            title: l10n.translate('permission_battery_title'),
            desc: l10n.translate('permission_battery_desc'),
            isGranted: _hasBatteryOptimization,
            onTap: () async {
              await NativeLockBridge.requestIgnoreBatteryOptimization();
              await _refreshPermissions();
            },
          ),
          const SizedBox(height: 24),

          // Language Setting
          const Text(
            'اللغة والمظهر',
            style: TextStyle(
              color: AppTheme.textPrimary,
              fontSize: 16,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 12),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: AppTheme.bgSurface2,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppTheme.borderSubtle),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(l10n.translate('language_setting'), style: const TextStyle(color: AppTheme.textPrimary)),
                DropdownButton<String>(
                  value: widget.storageService.getLocale(),
                  dropdownColor: AppTheme.bgSurface2,
                  underline: const SizedBox.shrink(),
                  items: const [
                    DropdownMenuItem(value: 'ar', child: Text('العربية')),
                    DropdownMenuItem(value: 'en', child: Text('English')),
                  ],
                  onChanged: (lang) {
                    if (lang != null) {
                      widget.storageService.setLocale(lang);
                      widget.onLocaleChanged(Locale(lang));
                      setState(() {});
                    }
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),

          // Theme Setting
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: AppTheme.bgSurface2,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppTheme.borderSubtle),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(l10n.translate('theme_setting'), style: const TextStyle(color: AppTheme.textPrimary)),
                Switch(
                  value: widget.storageService.isDarkMode(),
                  activeColor: AppTheme.accentPrimary,
                  onChanged: (isDark) {
                    widget.storageService.setDarkMode(isDark);
                    widget.onThemeModeChanged(isDark);
                    setState(() {});
                  },
                ),
              ],
            ),
          ),
          const SizedBox(height: 32),

          // Community & License Footer
          Center(
            child: Column(
              children: [
                InkWell(
                  onTap: () => launchUrl(
                    Uri.parse('https://discord.gg/PufJHjaAKJ'),
                    mode: LaunchMode.externalApplication,
                  ),
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                    decoration: BoxDecoration(
                      color: const Color(0xFF5865F2).withOpacity(0.15),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: const Color(0xFF5865F2).withOpacity(0.4)),
                    ),
                    child: const Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.forum_rounded, color: Color(0xFF5865F2), size: 18),
                        SizedBox(width: 8),
                        Text(
                          'انضم لمجتمع ديسكورد الدعم والمساعدة',
                          style: TextStyle(
                            color: Color(0xFF5865F2),
                            fontSize: 13,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  l10n.translate('copyright_notice'),
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: AppTheme.textMuted, fontSize: 11),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildPermissionTile({
    required String title,
    required String desc,
    required bool isGranted,
    required VoidCallback onTap,
  }) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppTheme.bgSurface2,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: isGranted ? AppTheme.borderSubtle : AppTheme.warning),
      ),
      child: Row(
        children: [
          Icon(
            isGranted ? Icons.check_circle_rounded : Icons.error_outline_rounded,
            color: isGranted ? AppTheme.success : AppTheme.warning,
            size: 22,
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  title,
                  style: const TextStyle(
                    color: AppTheme.textPrimary,
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                  ),
                ),
                Text(desc, style: const TextStyle(color: AppTheme.textSecondary, fontSize: 11)),
              ],
            ),
          ),
          const SizedBox(width: 8),
          TextButton(
            onPressed: onTap,
            child: Text(
              isGranted ? 'مفعل' : 'تفعيل',
              style: TextStyle(
                color: isGranted ? AppTheme.textMuted : AppTheme.accentPrimary,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
    );
  }
}
