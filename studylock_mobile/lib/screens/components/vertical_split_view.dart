import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';

class VerticalSplitView extends StatefulWidget {
  final Widget top;
  final Widget bottom;
  final double initialRatio;

  const VerticalSplitView({
    super.key,
    required this.top,
    required this.bottom,
    this.initialRatio = 0.50,
  });

  @override
  State<VerticalSplitView> createState() => _VerticalSplitViewState();
}

class _VerticalSplitViewState extends State<VerticalSplitView> {
  late double _ratio;
  final double _dividerHeight = 28.0;
  final double _minRatio = 0.15;
  final double _maxRatio = 0.85;

  @override
  void initState() {
    super.initState();
    _ratio = widget.initialRatio;
  }

  void _resetRatio() {
    setState(() {
      _ratio = 0.50;
    });
  }

  void _maximizeTop() {
    setState(() {
      _ratio = _maxRatio;
    });
  }

  void _maximizeBottom() {
    setState(() {
      _ratio = _minRatio;
    });
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final totalHeight = constraints.maxHeight - _dividerHeight;
        final topHeight = totalHeight * _ratio;
        final bottomHeight = totalHeight * (1.0 - _ratio);

        return Column(
          children: [
            // Top Pane (Web View)
            SizedBox(
              height: topHeight,
              width: constraints.maxWidth,
              child: widget.top,
            ),

            // Draggable Divider Bar
            GestureDetector(
              behavior: HitTestBehavior.opaque,
              onDoubleTap: _resetRatio,
              onVerticalDragUpdate: (details) {
                setState(() {
                  _ratio += details.primaryDelta! / totalHeight;
                  _ratio = _ratio.clamp(_minRatio, _maxRatio);
                });
              },
              child: Container(
                height: _dividerHeight,
                width: double.infinity,
                decoration: const BoxDecoration(
                  color: AppTheme.bgSurface2,
                  border: Border.symmetric(
                    horizontal: BorderSide(
                      color: AppTheme.borderSubtle,
                      width: 1,
                    ),
                  ),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    // Quick collapse bottom
                    IconButton(
                      icon: const Icon(Icons.arrow_drop_down, size: 18),
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(),
                      color: AppTheme.textMuted,
                      onPressed: _maximizeTop,
                      tooltip: 'Maximize Web',
                    ),
                    const SizedBox(width: 8),

                    // Center Grip Pill
                    Container(
                      width: 44,
                      height: 5,
                      decoration: BoxDecoration(
                        color: AppTheme.accentPrimary.withOpacity(0.8),
                        borderRadius: BorderRadius.circular(10),
                      ),
                    ),
                    const SizedBox(width: 8),

                    // Quick collapse top
                    IconButton(
                      icon: const Icon(Icons.arrow_drop_up, size: 18),
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(),
                      color: AppTheme.textMuted,
                      onPressed: _maximizeBottom,
                      tooltip: 'Maximize PDF',
                    ),
                  ],
                ),
              ),
            ),

            // Bottom Pane (PDF Viewer)
            SizedBox(
              height: bottomHeight,
              width: constraints.maxWidth,
              child: widget.bottom,
            ),
          ],
        );
      },
    );
  }
}
