using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Dissolvers88A.Graphing;
using Dissolvers88A.ViewModels;

namespace Dissolvers88A.Controls;

/// <summary>Draws the coordinate plane and every enabled Y= curve; drag to pan, wheel to zoom.</summary>
public sealed class GraphSurface : FrameworkElement
{
    private GrapherViewModel? _vm;
    public GrapherViewModel? Grapher
    {
        get => _vm;
        set
        {
            if (_vm != null) _vm.GraphChanged -= InvalidateVisual;
            _vm = value;
            if (_vm != null) _vm.GraphChanged += InvalidateVisual;
            InvalidateVisual();
        }
    }

    /// <summary>Fired after a pan or zoom so the WINDOW fields can refresh.</summary>
    public event Action? ViewportChanged;

    private static readonly Brush Bg = Frozen("#0E1626");
    private static readonly Pen GridPen = FrozenPen("#1C2A45", 1);
    private static readonly Pen GridPenMinor = FrozenPen("#161F35", 1);
    private static readonly Pen AxisPen = FrozenPen("#4B5D80", 1.4);
    private static readonly Brush LabelBrush = Frozen("#8CA0C4");
    private static readonly Brush TraceBrush = Frozen("#FDE68A");
    private readonly Typeface _face = new("Consolas");

    private Point _lastMouse;
    private bool _panning;

    public GraphSurface()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        if (_vm != null)
        {
            _vm.Viewport.PixelWidth = ActualWidth;
            _vm.Viewport.PixelHeight = ActualHeight;
        }
        InvalidateVisual();
    }

    // ---- interaction ---------------------------------------------------

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        _lastMouse = e.GetPosition(this);
        _panning = true;
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!_panning || _vm == null) return;
        var p = e.GetPosition(this);
        _vm.Viewport.PanByPixels(p.X - _lastMouse.X, p.Y - _lastMouse.Y);
        _lastMouse = p;
        InvalidateVisual();
        ViewportChanged?.Invoke();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        _panning = false;
        ReleaseMouseCapture();
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        if (_vm == null) return;
        var p = e.GetPosition(this);
        _vm.Viewport.ZoomAt(p.X, p.Y, e.Delta > 0 ? 0.85 : 1.0 / 0.85);
        InvalidateVisual();
        ViewportChanged?.Invoke();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_vm is not { Tracing: true }) return;
        switch (e.Key)
        {
            case Key.Left: _vm.TraceStep(-1); e.Handled = true; break;
            case Key.Right: _vm.TraceStep(1); e.Handled = true; break;
            case Key.Up: _vm.TraceSwitchFunction(-1); e.Handled = true; break;
            case Key.Down: _vm.TraceSwitchFunction(1); e.Handled = true; break;
            case Key.Escape: _vm.StopTrace(); e.Handled = true; break;
        }
    }

    // ---- rendering ----------------------------------------------------

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth, h = ActualHeight;
        dc.DrawRectangle(Bg, null, new Rect(0, 0, w, h));
        if (_vm == null || w < 2 || h < 2) return;

        var vp = _vm.Viewport;
        vp.PixelWidth = w;
        vp.PixelHeight = h;

        DrawGrid(dc, vp, w, h);
        DrawAxes(dc, vp, w, h);

        foreach (var fn in _vm.Functions)
            if (fn.IsPlottable)
                DrawCurve(dc, vp, fn, w, h);

        if (_vm.PlotKind != ViewModels.StatPlotKind.Off)
            DrawStatPlot(dc, vp, w, h);

        DrawTrace(dc, vp, w, h);
    }

    private static readonly Brush StatBrush = Frozen("#38BDF8");

    private void DrawStatPlot(DrawingContext dc, Viewport vp, double w, double h)
    {
        var data = _vm!.StatData;
        var xs = data[_vm.PlotXList];
        if (xs.Count == 0) return;
        var pen = new Pen(StatBrush, 1.6); pen.Freeze();

        switch (_vm.PlotKind)
        {
            case ViewModels.StatPlotKind.Scatter:
            case ViewModels.StatPlotKind.XyLine:
            {
                var ys = data[_vm.PlotYList];
                int n = Math.Min(xs.Count, ys.Count);
                Point? prev = null;
                for (int i = 0; i < n; i++)
                {
                    double px = vp.ScreenX(xs[i]), py = vp.ScreenY(ys[i]);
                    if (_vm.PlotKind == ViewModels.StatPlotKind.XyLine && prev is { } p)
                        dc.DrawLine(pen, p, new Point(px, py));
                    dc.DrawRectangle(StatBrush, null, new Rect(px - 2.5, py - 2.5, 5, 5));
                    prev = new Point(px, py);
                }
                break;
            }
            case ViewModels.StatPlotKind.Histogram:
            {
                double lo = xs.Min(), hi = xs.Max();
                if (hi <= lo) hi = lo + 1;
                int bins = Math.Clamp((int)Math.Sqrt(xs.Count) + 1, 4, 20);
                double bw = (hi - lo) / bins;
                var counts = new int[bins];
                foreach (var v in xs) counts[Math.Clamp((int)((v - lo) / bw), 0, bins - 1)]++;
                int maxC = counts.Max();
                double baseY = vp.ScreenY(0);
                for (int b = 0; b < bins; b++)
                {
                    double x0 = vp.ScreenX(lo + b * bw), x1 = vp.ScreenX(lo + (b + 1) * bw);
                    double barH = counts[b] / (double)maxC * (h * 0.6);
                    var rect = new Rect(x0, baseY - barH, Math.Max(1, x1 - x0 - 1), barH);
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(90, 56, 189, 248)), pen, rect);
                }
                break;
            }
            case ViewModels.StatPlotKind.Box:
            {
                var s = Engine.Stats.OneVar(xs);
                double y = h * 0.5, box = h * 0.12;
                double xa = vp.ScreenX(s.Min), xq1 = vp.ScreenX(s.Q1), xm = vp.ScreenX(s.Median),
                       xq3 = vp.ScreenX(s.Q3), xb = vp.ScreenX(s.Max);
                dc.DrawLine(pen, new Point(xa, y), new Point(xq1, y));
                dc.DrawLine(pen, new Point(xq3, y), new Point(xb, y));
                dc.DrawLine(pen, new Point(xa, y - box / 2), new Point(xa, y + box / 2));
                dc.DrawLine(pen, new Point(xb, y - box / 2), new Point(xb, y + box / 2));
                dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 56, 189, 248)), pen,
                    new Rect(xq1, y - box, xq3 - xq1, box * 2));
                dc.DrawLine(new Pen(StatBrush, 2.4), new Point(xm, y - box), new Point(xm, y + box));
                break;
            }
        }
    }

    private void DrawGrid(DrawingContext dc, Viewport vp, double w, double h)
    {
        double xscl = vp.XScl > 0 ? vp.XScl : 1;
        double yscl = vp.YScl > 0 ? vp.YScl : 1;

        // keep the grid from becoming a solid block when zoomed far out
        while (vp.SpanX / xscl > 40) xscl *= 2;
        while (vp.SpanY / yscl > 40) yscl *= 2;

        double startX = Math.Ceiling(vp.XMin / xscl) * xscl;
        for (double x = startX; x <= vp.XMax; x += xscl)
        {
            double px = vp.ScreenX(x);
            dc.DrawLine(GridPen, new Point(px, 0), new Point(px, h));
        }
        double startY = Math.Ceiling(vp.YMin / yscl) * yscl;
        for (double y = startY; y <= vp.YMax; y += yscl)
        {
            double py = vp.ScreenY(y);
            dc.DrawLine(GridPen, new Point(0, py), new Point(w, py));
        }
    }

    private void DrawAxes(DrawingContext dc, Viewport vp, double w, double h)
    {
        double x0 = vp.ScreenX(0);
        double y0 = vp.ScreenY(0);
        bool yAxisVisible = x0 >= 0 && x0 <= w;
        bool xAxisVisible = y0 >= 0 && y0 <= h;

        if (yAxisVisible) dc.DrawLine(AxisPen, new Point(x0, 0), new Point(x0, h));
        if (xAxisVisible) dc.DrawLine(AxisPen, new Point(0, y0), new Point(w, y0));

        double xscl = vp.XScl > 0 ? vp.XScl : 1;
        double yscl = vp.YScl > 0 ? vp.YScl : 1;
        while (vp.SpanX / xscl > 20) xscl *= 2;
        while (vp.SpanY / yscl > 20) yscl *= 2;

        double labelY = Math.Clamp(y0 + 4, 2, h - 16);
        for (double x = Math.Ceiling(vp.XMin / xscl) * xscl; x <= vp.XMax; x += xscl)
        {
            if (Math.Abs(x) < xscl / 2) continue;
            DrawLabel(dc, Trim(x), vp.ScreenX(x) + 3, labelY);
        }
        double labelX = Math.Clamp(x0 + 5, 2, w - 40);
        for (double y = Math.Ceiling(vp.YMin / yscl) * yscl; y <= vp.YMax; y += yscl)
        {
            if (Math.Abs(y) < yscl / 2) continue;
            DrawLabel(dc, Trim(y), labelX, vp.ScreenY(y) - 8);
        }
    }

    private void DrawCurve(DrawingContext dc, Viewport vp, Graphing.GraphFunction fn, double w, double h)
    {
        var pen = new Pen(fn.Brush, 2) { LineJoin = PenLineJoin.Round };
        pen.Freeze();

        var geo = new StreamGeometry();
        double breakGap = h * 3;
        using (var ctx = geo.Open())
        {
            bool drawing = false;
            double prevPy = 0;
            for (double px = 0; px <= w; px += 1)
            {
                double x = vp.WorldX(px);
                double y = _vm!.Evaluate(fn, x);
                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    drawing = false;
                    continue;
                }
                double py = vp.ScreenY(y);
                if (!drawing)
                {
                    ctx.BeginFigure(new Point(px, py), false, false);
                    drawing = true;
                }
                else if (Math.Abs(py - prevPy) > breakGap)
                {
                    // likely a vertical asymptote — lift the pen
                    ctx.BeginFigure(new Point(px, py), false, false);
                }
                else
                {
                    ctx.LineTo(new Point(px, py), true, false);
                }
                prevPy = py;
            }
        }
        geo.Freeze();
        dc.DrawGeometry(null, pen, geo);
    }

    private void DrawTrace(DrawingContext dc, Viewport vp, double w, double h)
    {
        if (_vm is not { Tracing: true } vm) return;
        var fn = vm.TraceFunction;
        if (fn == null || !fn.IsPlottable) return;

        double x = vm.TraceX;
        double y = vm.Evaluate(fn, x);
        double px = vp.ScreenX(x);
        var cross = new Pen(TraceBrush, 1) { DashStyle = DashStyles.Dash };
        cross.Freeze();
        dc.DrawLine(cross, new Point(px, 0), new Point(px, h));

        if (!double.IsNaN(y) && !double.IsInfinity(y))
        {
            double py = vp.ScreenY(y);
            dc.DrawLine(cross, new Point(0, py), new Point(w, py));
            dc.DrawEllipse(TraceBrush, null, new Point(px, py), 4, 4);
        }

        string text = $"{fn.Label}   X={Engine.Calculator.Format(x)}   Y={Engine.Calculator.Format(y)}";
        var ft = MakeText(text, 12.5, Brushes.White);
        dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(210, 8, 14, 26)), null,
            new Rect(8, h - ft.Height - 12, ft.Width + 16, ft.Height + 8));
        dc.DrawText(ft, new Point(16, h - ft.Height - 8));
    }

    // ---- text helpers ------------------------------------------------

    private void DrawLabel(DrawingContext dc, string s, double x, double y)
        => dc.DrawText(MakeText(s, 11, LabelBrush), new Point(x, y));

    private FormattedText MakeText(string s, double size, Brush brush)
    {
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        return new FormattedText(s, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _face, size, brush, dpi);
    }

    private static string Trim(double v)
    {
        string s = v.ToString("0.####", CultureInfo.InvariantCulture);
        return s;
    }

    private static Brush Frozen(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    private static Pen FrozenPen(string hex, double thickness)
    {
        var p = new Pen(Frozen(hex), thickness);
        p.Freeze();
        return p;
    }
}
