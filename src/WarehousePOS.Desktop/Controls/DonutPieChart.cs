using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WarehousePOS.Desktop.ViewModels.Expenses;

namespace WarehousePOS.Desktop.Controls;

/// <summary>
/// A lightweight, native WPF donut/pie chart that renders colored annular slices
/// corresponding to category expense types and their percentages.
/// </summary>
public sealed class DonutPieChart : FrameworkElement
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(DonutPieChart),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

    public static readonly DependencyProperty InnerRadiusRatioProperty =
        DependencyProperty.Register(
            nameof(InnerRadiusRatio),
            typeof(double),
            typeof(DonutPieChart),
            new FrameworkPropertyMetadata(0.6923, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty EmptyBrushProperty =
        DependencyProperty.Register(
            nameof(EmptyBrush),
            typeof(Brush),
            typeof(DonutPieChart),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(226, 232, 240))));

    public static readonly DependencyProperty StrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(DonutPieChart),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeBrushProperty =
        DependencyProperty.Register(
            nameof(StrokeBrush),
            typeof(Brush),
            typeof(DonutPieChart),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double InnerRadiusRatio
    {
        get => (double)GetValue(InnerRadiusRatioProperty);
        set => SetValue(InnerRadiusRatioProperty, value);
    }

    public Brush EmptyBrush
    {
        get => (Brush)GetValue(EmptyBrushProperty);
        set => SetValue(EmptyBrushProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public Brush StrokeBrush
    {
        get => (Brush)GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    private readonly List<(double StartAngle, double EndAngle, CategoryAnalyticsItemViewModel Item)> _renderedSlices = [];

    public DonutPieChart()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DonutPieChart chart)
        {
            if (e.OldValue is INotifyCollectionChanged oldCol)
            {
                oldCol.CollectionChanged -= chart.OnCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCol)
            {
                newCol.CollectionChanged += chart.OnCollectionChanged;
            }

            chart.InvalidateVisual();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ItemsSource is INotifyCollectionChanged col)
        {
            col.CollectionChanged -= OnCollectionChanged;
            col.CollectionChanged += OnCollectionChanged;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ItemsSource is INotifyCollectionChanged col)
        {
            col.CollectionChanged -= OnCollectionChanged;
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_renderedSlices.Count == 0)
        {
            ToolTip = null;
            return;
        }

        Point pos = e.GetPosition(this);
        double cx = ActualWidth / 2.0;
        double cy = ActualHeight / 2.0;
        double dx = pos.X - cx;
        double dy = pos.Y - cy;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        double outerRadius = Math.Min(ActualWidth, ActualHeight) / 2.0;
        double innerRadius = outerRadius * InnerRadiusRatio;

        if (dist >= innerRadius && dist <= outerRadius)
        {
            double angleDeg = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angleDeg < -90.0)
            {
                angleDeg += 360.0;
            }

            var matched = _renderedSlices.FirstOrDefault(s => angleDeg >= s.StartAngle && angleDeg <= s.EndAngle);
            if (matched.Item != null)
            {
                ToolTip = $"{matched.Item.CategoryName}: {matched.Item.TotalAmountFormatted} ({matched.Item.Percentage:F1}%)";
                return;
            }
        }

        ToolTip = null;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ToolTip = null;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        _renderedSlices.Clear();

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        Point center = new Point(width / 2.0, height / 2.0);
        double outerRadius = Math.Min(width, height) / 2.0;
        double innerRadius = outerRadius * InnerRadiusRatio;

        var items = ItemsSource?.OfType<CategoryAnalyticsItemViewModel>()
                                .Where(i => i.TotalAmount > 0)
                                .OrderByDescending(i => i.TotalAmount)
                                .ToList();

        decimal totalAmount = items?.Sum(i => i.TotalAmount) ?? 0m;

        if (items == null || items.Count == 0 || totalAmount <= 0m)
        {
            // Draw empty neutral ring
            var emptyRing = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new EllipseGeometry(center, outerRadius, outerRadius),
                new EllipseGeometry(center, innerRadius, innerRadius));
            emptyRing.Freeze();
            dc.DrawGeometry(EmptyBrush, null, emptyRing);
            return;
        }

        if (items.Count == 1)
        {
            // Single 100% slice
            var singleItem = items[0];
            var fullRing = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new EllipseGeometry(center, outerRadius, outerRadius),
                new EllipseGeometry(center, innerRadius, innerRadius));
            fullRing.Freeze();
            dc.DrawGeometry(singleItem.ColorBrush, null, fullRing);
            _renderedSlices.Add((-90.0, 270.0, singleItem));
            return;
        }

        // Multiple slices: calculate sweep angles ensuring minimum visibility for small slices
        const double minAngle = 3.0; // Minimum 3 degrees so tiny expenses remain clearly visible
        var rawAngles = items.Select(i => (double)(i.TotalAmount / totalAmount) * 360.0).ToList();

        // Check if any slice is smaller than minAngle
        double[] finalAngles = new double[items.Count];
        double allocatedSmall = 0.0;
        double nonSmallRawSum = 0.0;
        int smallCount = 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (rawAngles[i] < minAngle)
            {
                smallCount++;
                allocatedSmall += minAngle;
            }
            else
            {
                nonSmallRawSum += rawAngles[i];
            }
        }

        if (smallCount > 0 && nonSmallRawSum > 0.0 && allocatedSmall < 360.0)
        {
            double scale = (360.0 - allocatedSmall) / nonSmallRawSum;
            for (int i = 0; i < items.Count; i++)
            {
                finalAngles[i] = rawAngles[i] < minAngle ? minAngle : rawAngles[i] * scale;
            }
        }
        else
        {
            for (int i = 0; i < items.Count; i++)
            {
                finalAngles[i] = rawAngles[i];
            }
        }

        Pen borderPen = new Pen(StrokeBrush, StrokeThickness);
        borderPen.Freeze();

        double currentAngle = -90.0; // Start at 12 o'clock
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            double sweep = finalAngles[i];
            double startAngle = currentAngle;
            double endAngle = (i == items.Count - 1) ? 270.0 : currentAngle + sweep;

            var sector = CreateAnnularSector(center, innerRadius, outerRadius, startAngle, endAngle);
            dc.DrawGeometry(item.ColorBrush, borderPen, sector);

            _renderedSlices.Add((startAngle, endAngle, item));
            currentAngle = endAngle;
        }
    }

    private static Geometry CreateAnnularSector(Point center, double innerRadius, double outerRadius, double startAngleDeg, double endAngleDeg)
    {
        double startRad = startAngleDeg * Math.PI / 180.0;
        double endRad = endAngleDeg * Math.PI / 180.0;
        double sweepDeg = endAngleDeg - startAngleDeg;

        Point p1 = new Point(center.X + outerRadius * Math.Cos(startRad), center.Y + outerRadius * Math.Sin(startRad));
        Point p2 = new Point(center.X + outerRadius * Math.Cos(endRad),   center.Y + outerRadius * Math.Sin(endRad));
        Point p3 = new Point(center.X + innerRadius * Math.Cos(endRad),   center.Y + innerRadius * Math.Sin(endRad));
        Point p4 = new Point(center.X + innerRadius * Math.Cos(startRad), center.Y + innerRadius * Math.Sin(startRad));

        bool isLargeArc = sweepDeg > 180.0;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(p1, isFilled: true, isClosed: true);
            ctx.ArcTo(p2, new Size(outerRadius, outerRadius), 0, isLargeArc, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
            ctx.LineTo(p3, isStroked: true, isSmoothJoin: false);
            ctx.ArcTo(p4, new Size(innerRadius, innerRadius), 0, isLargeArc, SweepDirection.Counterclockwise, isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        return geometry;
    }
}
