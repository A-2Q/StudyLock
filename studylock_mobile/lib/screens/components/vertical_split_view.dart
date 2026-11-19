import 'package:flutter/material.dart';
import '../../core/theme/app_theme.dart';

/// Adaptive split view that switches between:
/// - Side-by-Side (Horizontal) layout on tablets and in landscape orientation
/// - Stacked (Vertical) layout on phones in portrait orientation
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
  final double _dividerSize = 28.0;
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

  void _maximizeFirst() {
    setState(() {
      _ratio = _maxRatio;
    });
  }

  void _maximizeSecond() {
    setState(() {
      _ratio = _minRatio;
    });
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final isLandscapeOrTablet = constraints.maxWidth > constraints.maxHeight ||
            constraints.maxWidth >= 720;

        if (isLandscapeOrTablet) {
          // Tablet & Landscape Mode: Side-by-Side (Horizontal Split)
          final totalWidth = constraints.maxWidth - _dividerSize;
          final leftWidth = totalWidth * _ratio;
          final rightWidth = totalWidth * (1.0 - _ratio);

          return Row(
            children: [
              // Left Pane (Web View)
              SizedBox(
                width: leftWidth,
                height: constraints.maxHeight,
                child: widget.top,
              ),

              // Vertical Draggable Divider Bar
              GestureDetector(
                behavior: HitTestBehavior.opaque,
                onDoubleTap: _resetRatio,
                onHorizontalDragUpdate: (details) {
                  setState(() {
                    _ratio += details.primaryDelta! / totalWidth;
                    _ratio = _ratio.clamp(_minRatio, _maxRatio);
                  });
                },
                child: Container(
                  width: _dividerSize,
                  height: double.infinity,
                  decoration: const BoxDecoration(
                    color: AppTheme.bgSurface2,
                    border: Border.symmetric(
                      vertical: BorderSide(
                        color: AppTheme.borderSubtle,
                        width: 1,
                      ),
                    ),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      // Quick expand left
                      IconButton(
                        icon: const Icon(Icons.arrow_left, size: 18),
                        padding: EdgeInsets.zero,
                        constraints: const BoxConstraints(),
                        color: AppTheme.textMuted,
                        onPressed: _maximizeFirst,
                        tooltip: 'Maximize Web',
                      ),
                      const SizedBox(height: 8),

                      // Center Grip Pill (Vertical)
                      Container(
                        width: 5,
                        height: 44,
                        decoration: BoxDecoration(
                          color: AppTheme.accentPrimary.withOpacity(0.8),
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                      const SizedBox(height: 8),

                      // Quick expand right
                      IconButton(
                        icon: const Icon(Icons.arrow_right, size: 18),
                        padding: EdgeInsets.zero,
                        constraints: const BoxConstraints(),
                        color: AppTheme.textMuted,
                        onPressed: _maximizeSecond,
                        tooltip: 'Maximize PDF',
                      ),
                    ],
                  ),
                ),
              ),

              // Right Pane (PDF Viewer)
              SizedBox(
                width: rightWidth,
                height: constraints.maxHeight,
                child: widget.bottom,
              ),
            ],
          );
        } else {
          // Phone Portrait Mode: Top / Bottom Split
          final totalHeight = constraints.maxHeight - _dividerSize;
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

              // Horizontal Draggable Divider Bar
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
                  height: _dividerSize,
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
                      IconButton(
                        icon: const Icon(Icons.arrow_drop_down, size: 18),
                        padding: EdgeInsets.zero,
                        constraints: const BoxConstraints(),
                        color: AppTheme.textMuted,
                        onPressed: _maximizeFirst,
                        tooltip: 'Maximize Web',
                      ),
                      const SizedBox(width: 8),

                      // Center Grip Pill (Horizontal)
                      Container(
                        width: 44,
                        height: 5,
                        decoration: BoxDecoration(
                          color: AppTheme.accentPrimary.withOpacity(0.8),
                          borderRadius: BorderRadius.circular(10),
                        ),
                      ),
                      const SizedBox(width: 8),

                      IconButton(
                        icon: const Icon(Icons.arrow_drop_up, size: 18),
                        padding: EdgeInsets.zero,
                        constraints: const BoxConstraints(),
                        color: AppTheme.textMuted,
                        onPressed: _maximizeSecond,
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
        }
      },
    );
  }
}
