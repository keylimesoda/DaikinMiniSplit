using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace DaikinManagerV2.Controls;

using Path = Microsoft.UI.Xaml.Shapes.Path;

/// <summary>
/// A beautiful Fluent Design temperature dial with mode-based coloring,
/// smooth animations, and tactile interactions.
/// </summary>
public sealed class TemperatureDial : UserControl
{
    // Visual elements
    private Canvas? _canvas;
    private Ellipse? _knob;
    private Ellipse? _knobShadow;
    private TextBlock? _tempText;
    private TextBlock? _unitText;
    private Path? _trackPath;
    private Path? _progressPath;
    private Grid? _rootGrid;
    
    // Composition for animations
    private Compositor? _compositor;
    
    // State
    private bool _isDragging;
    private bool _isKnobHovered;
    private double _animatedTemperature;
    private Color _currentColor;

    // Constants
    private const double CenterX = 140;
    private const double CenterY = 140;
    private const double Radius = 110;
    private const double StartAngle = 135; // Bottom-left
    private const double SweepAngle = 270; // To bottom-right
    private const double KnobSize = 44;
    private const double KnobShadowSize = 48;

    #region Dependency Properties

    public static readonly DependencyProperty TemperatureProperty =
        DependencyProperty.Register(nameof(Temperature), typeof(double), typeof(TemperatureDial),
            new PropertyMetadata(72.0, OnTemperatureChanged));

    public static readonly DependencyProperty MinTemperatureProperty =
        DependencyProperty.Register(nameof(MinTemperature), typeof(double), typeof(TemperatureDial),
            new PropertyMetadata(60.0));

    public static readonly DependencyProperty MaxTemperatureProperty =
        DependencyProperty.Register(nameof(MaxTemperature), typeof(double), typeof(TemperatureDial),
            new PropertyMetadata(90.0));

    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(nameof(Mode), typeof(DialMode), typeof(TemperatureDial),
            new PropertyMetadata(DialMode.Cool, OnModeChanged));

    public double Temperature
    {
        get => (double)GetValue(TemperatureProperty);
        set => SetValue(TemperatureProperty, Math.Clamp(value, MinTemperature, MaxTemperature));
    }

    public double MinTemperature
    {
        get => (double)GetValue(MinTemperatureProperty);
        set => SetValue(MinTemperatureProperty, value);
    }

    public double MaxTemperature
    {
        get => (double)GetValue(MaxTemperatureProperty);
        set => SetValue(MaxTemperatureProperty, value);
    }

    public DialMode Mode
    {
        get => (DialMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    #endregion

    public TemperatureDial()
    {
        DefaultStyleKey = typeof(TemperatureDial);
        _animatedTemperature = 72.0;
        _currentColor = Colors.DodgerBlue;
        
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
        
        // Enable keyboard focus
        this.IsTabStop = true;
        this.UseSystemFocusVisuals = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildVisualTree();
        SetupCompositionAnimations();
        _animatedTemperature = Temperature;
        _currentColor = GetModeColor();  // Apply current mode's color
        UpdateVisuals(animate: false);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Cleanup if needed
    }

    private static void OnTemperatureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TemperatureDial dial && dial._canvas != null)
        {
            dial.AnimateToTemperature((double)e.NewValue);
        }
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TemperatureDial dial && dial._canvas != null)
        {
            dial._currentColor = dial.GetModeColor();
            dial.UpdateVisuals(animate: false);
        }
    }

    private void SetupCompositionAnimations()
    {
        if (_knob == null) return;
        _compositor = ElementCompositionPreview.GetElementVisual(_knob).Compositor;
    }

    private void BuildVisualTree()
    {
        _rootGrid = new Grid
        {
            Width = 280,
            Height = 280,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _canvas = new Canvas
        {
            Width = 280,
            Height = 280
        };

        // Background track (theme-aware gray arc)
        _trackPath = new Path
        {
            Stroke = (Brush)Application.Current.Resources["ControlStrongStrokeColorDefaultBrush"] 
                     ?? new SolidColorBrush(Colors.LightGray),
            StrokeThickness = 20,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Opacity = 0.3
        };
        _canvas.Children.Add(_trackPath);

        // Progress arc (colored, with subtle gradient effect via opacity)
        _progressPath = new Path
        {
            StrokeThickness = 20,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        _canvas.Children.Add(_progressPath);

        // Knob shadow (for depth)
        _knobShadow = new Ellipse
        {
            Width = KnobShadowSize,
            Height = KnobShadowSize,
            Fill = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
            // Offset slightly for shadow effect
        };
        _canvas.Children.Add(_knobShadow);

        // Knob (the draggable handle)
        _knob = new Ellipse
        {
            Width = KnobSize,
            Height = KnobSize,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 4,
            // Add subtle shadow via composition
        };
        _canvas.Children.Add(_knob);

        // Setup knob hover/press animations
        SetupKnobInteractions();

        // Center content (temperature display)
        var centerStack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = -4
        };

        _tempText = new TextBlock
        {
            FontSize = 52,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            CharacterSpacing = -20
        };

        _unitText = new TextBlock
        {
            Text = "°F",
            FontSize = 20,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Opacity = 0.7
        };

        centerStack.Children.Add(_tempText);
        centerStack.Children.Add(_unitText);

        _rootGrid.Children.Add(_canvas);
        _rootGrid.Children.Add(centerStack);

        // Input handling
        _canvas.PointerPressed += Canvas_PointerPressed;
        _canvas.PointerMoved += Canvas_PointerMoved;
        _canvas.PointerReleased += Canvas_PointerReleased;
        _canvas.PointerCaptureLost += Canvas_PointerCaptureLost;
        _canvas.PointerEntered += Canvas_PointerEntered;
        _canvas.PointerExited += Canvas_PointerExited;

        // Keyboard support
        this.KeyDown += OnKeyDown;

        this.Content = _rootGrid;
    }

    private void SetupKnobInteractions()
    {
        if (_knob == null) return;
        
        // Add hover effect to the knob
        _knob.PointerEntered += (s, e) =>
        {
            if (_knob == null || _isKnobHovered) return;
            _isKnobHovered = true;
            
            // Scale up slightly and add glow effect
            _knob.StrokeThickness = 5;
            _knob.Width = KnobSize + 4;
            _knob.Height = KnobSize + 4;
            
            // Reposition to keep centered
            var left = Canvas.GetLeft(_knob);
            var top = Canvas.GetTop(_knob);
            Canvas.SetLeft(_knob, left - 2);
            Canvas.SetTop(_knob, top - 2);
        };
        
        _knob.PointerExited += (s, e) =>
        {
            if (_knob == null || _isDragging || !_isKnobHovered) return;
            _isKnobHovered = false;
            
            // Restore normal size
            _knob.StrokeThickness = 4;
            _knob.Width = KnobSize;
            _knob.Height = KnobSize;
            
            // Restore position
            var left = Canvas.GetLeft(_knob);
            var top = Canvas.GetTop(_knob);
            Canvas.SetLeft(_knob, left + 2);
            Canvas.SetTop(_knob, top + 2);
        };
    }

    private void AnimateToTemperature(double targetTemp)
    {
        // Direct update, no animation
        _animatedTemperature = targetTemp;
        UpdateVisuals(animate: false);
    }

    private Color GetModeColor()
    {
        return Mode switch
        {
            DialMode.Heat => Color.FromArgb(255, 255, 87, 51),   // Warm orange-red
            DialMode.Cool => Color.FromArgb(255, 30, 144, 255),  // Dodger blue
            DialMode.Auto => Color.FromArgb(255, 60, 179, 113), // Medium sea green
            DialMode.Dry => Color.FromArgb(255, 255, 193, 7),   // Amber/gold
            DialMode.Fan => Color.FromArgb(255, 128, 128, 128), // Gray (temp disabled)
            _ => Color.FromArgb(255, 60, 179, 113)
        };
    }

    private void UpdateVisuals(bool animate = true)
    {
        if (_canvas == null || _trackPath == null || _progressPath == null || 
            _knob == null || _knobShadow == null || _tempText == null || _unitText == null)
            return;

        // Update track path
        _trackPath.Data = CreateArcGeometry(CenterX, CenterY, Radius, StartAngle, SweepAngle);

        // Calculate progress
        double range = MaxTemperature - MinTemperature;
        double pct = (_animatedTemperature - MinTemperature) / range;
        double progressSweep = SweepAngle * pct;

        // Update progress arc with current animated color
        var brush = new SolidColorBrush(_currentColor);
        _progressPath.Stroke = brush;
        _progressPath.Data = CreateArcGeometry(CenterX, CenterY, Radius, StartAngle, Math.Max(0.1, progressSweep));

        // Position knob
        double knobAngle = (StartAngle + progressSweep) * Math.PI / 180;
        double kx = CenterX + Radius * Math.Cos(knobAngle);
        double ky = CenterY + Radius * Math.Sin(knobAngle);
        
        Canvas.SetLeft(_knob, kx - KnobSize / 2);
        Canvas.SetTop(_knob, ky - KnobSize / 2);
        
        // Position shadow (offset down-right for realistic shadow)
        Canvas.SetLeft(_knobShadow, kx - KnobShadowSize / 2 + 2);
        Canvas.SetTop(_knobShadow, ky - KnobShadowSize / 2 + 3);
        
        _knob.Fill = brush;

        // Update text with color
        _tempText.Text = $"{Math.Round(_animatedTemperature)}";
        _tempText.Foreground = brush;
        _unitText.Foreground = brush;
    }

    private static Geometry CreateArcGeometry(double cx, double cy, double r, double startAngle, double sweepAngle)
    {
        if (sweepAngle <= 0)
            return new PathGeometry();

        double startRad = startAngle * Math.PI / 180;
        double endRad = (startAngle + sweepAngle) * Math.PI / 180;

        double x1 = cx + r * Math.Cos(startRad);
        double y1 = cy + r * Math.Sin(startRad);
        double x2 = cx + r * Math.Cos(endRad);
        double y2 = cy + r * Math.Sin(endRad);

        bool largeArc = sweepAngle > 180;

        var figure = new PathFigure
        {
            StartPoint = new Point(x1, y1),
            IsClosed = false
        };

        figure.Segments.Add(new ArcSegment
        {
            Point = new Point(x2, y2),
            Size = new Size(r, r),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = largeArc
        });

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    #region Input Handling

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _canvas?.CapturePointer(e.Pointer);
        UpdateTemperatureFromPointer(e);
        e.Handled = true;
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            UpdateTemperatureFromPointer(e);
            e.Handled = true;
        }
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _canvas?.ReleasePointerCapture(e.Pointer);
            ResetKnobHoverState();
            e.Handled = true;
        }
    }

    private void Canvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
        ResetKnobHoverState();
    }
    
    private void ResetKnobHoverState()
    {
        if (_knob == null) return;
        _isKnobHovered = false;
        _knob.StrokeThickness = 4;
        _knob.Width = KnobSize;
        _knob.Height = KnobSize;
        // Visuals will be repositioned by UpdateVisuals on next update
        UpdateVisuals(animate: false);
    }

    private void Canvas_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // Show we're interactive
        if (this.ProtectedCursor == null)
        {
            // Would set hand cursor here if available
        }
    }

    private void Canvas_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        // No animation needed
    }

    private void UpdateTemperatureFromPointer(PointerRoutedEventArgs e)
    {
        if (_canvas == null) return;

        var point = e.GetCurrentPoint(_canvas).Position;

        // Calculate angle from center
        double angle = Math.Atan2(point.Y - CenterY, point.X - CenterX) * 180 / Math.PI;

        // Convert to 0-270 range starting from bottom-left (135°)
        angle -= StartAngle;
        if (angle < 0) angle += 360;

        // Clamp to valid range
        if (angle > 315) angle = 0; // Past the gap at bottom
        if (angle > SweepAngle) angle = SweepAngle;

        // Convert to temperature
        double pct = angle / SweepAngle;
        double temp = MinTemperature + (pct * (MaxTemperature - MinTemperature));
        
        // Round to nearest degree
        temp = Math.Round(temp);
        
        if (temp != Temperature)
        {
            // Direct update during drag (no animation, for responsiveness)
            _animatedTemperature = temp;
            Temperature = temp;
            UpdateVisuals(animate: false);
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        double delta = 0;
        
        switch (e.Key)
        {
            case VirtualKey.Up:
            case VirtualKey.Right:
                delta = 1;
                break;
            case VirtualKey.Down:
            case VirtualKey.Left:
                delta = -1;
                break;
            case VirtualKey.PageUp:
                delta = 5;
                break;
            case VirtualKey.PageDown:
                delta = -5;
                break;
            case VirtualKey.Home:
                Temperature = MaxTemperature;
                e.Handled = true;
                return;
            case VirtualKey.End:
                Temperature = MinTemperature;
                e.Handled = true;
                return;
            default:
                return;
        }

        if (delta != 0)
        {
            Temperature = Math.Clamp(Temperature + delta, MinTemperature, MaxTemperature);
            e.Handled = true;
        }
    }

    #endregion
}

/// <summary>
/// The operating mode of the HVAC system, which determines dial color.
/// </summary>
public enum DialMode
{
    Cool,
    Heat,
    Auto,
    Dry,
    Fan
}
