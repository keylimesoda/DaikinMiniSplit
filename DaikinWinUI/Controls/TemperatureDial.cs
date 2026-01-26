using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using DaikinManager.Services;
using Windows.Foundation;

namespace DaikinManager.Controls;

using Path = Microsoft.UI.Xaml.Shapes.Path;

public sealed class TemperatureDial : UserControl
{
    private Canvas? _canvas;
    private Ellipse? _knob;
    private TextBlock? _tempText;
    private Path? _trackPath;
    private Path? _progressPath;
    private bool _isDragging;

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
        DependencyProperty.Register(nameof(Mode), typeof(DaikinMode), typeof(TemperatureDial),
            new PropertyMetadata(DaikinMode.Cool, OnModeChanged));

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

    public DaikinMode Mode
    {
        get => (DaikinMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public TemperatureDial()
    {
        DefaultStyleKey = typeof(TemperatureDial);
        this.Loaded += TemperatureDial_Loaded;
    }

    private void TemperatureDial_Loaded(object sender, RoutedEventArgs e)
    {
        BuildVisualTree();
    }

    private static void OnTemperatureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TemperatureDial dial)
            dial.UpdateVisuals();
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TemperatureDial dial)
            dial.UpdateVisuals();
    }

    private void BuildVisualTree()
    {
        // Clear existing content
        var grid = new Grid
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

        // Background track (gray arc)
        _trackPath = new Path
        {
            Stroke = new SolidColorBrush(Colors.LightGray),
            StrokeThickness = 18,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        _canvas.Children.Add(_trackPath);

        // Progress arc (colored)
        _progressPath = new Path
        {
            StrokeThickness = 18,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        _canvas.Children.Add(_progressPath);

        // Knob
        _knob = new Ellipse
        {
            Width = 40,
            Height = 40,
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 3
        };
        _canvas.Children.Add(_knob);

        // Temperature text (center)
        _tempText = new TextBlock
        {
            FontSize = 44,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        grid.Children.Add(_canvas);
        grid.Children.Add(_tempText);

        // Input handling
        _canvas.PointerPressed += Canvas_PointerPressed;
        _canvas.PointerMoved += Canvas_PointerMoved;
        _canvas.PointerReleased += Canvas_PointerReleased;
        _canvas.PointerCaptureLost += Canvas_PointerCaptureLost;

        this.Content = grid;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_canvas == null || _trackPath == null || _progressPath == null || _knob == null || _tempText == null)
            return;

        double cx = 140, cy = 140, r = 110;
        double startAngle = 135; // Bottom-left
        double sweepAngle = 270; // To bottom-right

        // Update track path
        _trackPath.Data = CreateArcGeometry(cx, cy, r, startAngle, sweepAngle);

        // Calculate progress
        double range = MaxTemperature - MinTemperature;
        double pct = (Temperature - MinTemperature) / range;
        double progressSweep = sweepAngle * pct;

        // Get mode color
        var color = Mode switch
        {
            DaikinMode.Heat => Colors.OrangeRed,
            DaikinMode.Cool => Colors.DodgerBlue,
            _ => Colors.MediumSeaGreen
        };

        _progressPath.Stroke = new SolidColorBrush(color);
        _progressPath.Data = CreateArcGeometry(cx, cy, r, startAngle, progressSweep);

        // Position knob
        double knobAngle = (startAngle + progressSweep) * Math.PI / 180;
        double kx = cx + r * Math.Cos(knobAngle);
        double ky = cy + r * Math.Sin(knobAngle);
        Canvas.SetLeft(_knob, kx - 20);
        Canvas.SetTop(_knob, ky - 20);
        _knob.Fill = new SolidColorBrush(color);

        // Update text
        _tempText.Text = $"{Math.Round(Temperature)}°F";
        _tempText.Foreground = new SolidColorBrush(color);
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

    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = true;
        _canvas?.CapturePointer(e.Pointer);
        UpdateTemperatureFromPointer(e);
    }

    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
            UpdateTemperatureFromPointer(e);
    }

    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
        _canvas?.ReleasePointerCapture(e.Pointer);
    }

    private void Canvas_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
    }

    private void UpdateTemperatureFromPointer(PointerRoutedEventArgs e)
    {
        if (_canvas == null) return;

        var point = e.GetCurrentPoint(_canvas).Position;
        double cx = 90, cy = 90;

        // Calculate angle from center
        double angle = Math.Atan2(point.Y - cy, point.X - cx) * 180 / Math.PI;

        // Convert to 0-270 range starting from bottom-left (135°)
        angle -= 135;
        if (angle < 0) angle += 360;

        // Clamp to valid range
        if (angle > 315) angle = 0; // Past the gap at bottom
        if (angle > 270) angle = 270;

        // Convert to temperature
        double pct = angle / 270;
        double temp = MinTemperature + (pct * (MaxTemperature - MinTemperature));
        Temperature = Math.Round(temp);
    }
}
