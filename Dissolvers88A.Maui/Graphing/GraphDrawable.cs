using System.Globalization;
using Dissolvers88A.Maui.ViewModels;

namespace Dissolvers88A.Maui.Graphing;

/// <summary>
/// Draws the coordinate plane and every enabled Y= curve onto a MAUI
/// <see cref="GraphicsView"/>. Port of the WPF app's GraphSurface.OnRender.
/// </summary>
public sealed class GraphDrawable : IDrawable
{
    private readonly GrapherViewModel _vm;

    public GraphDrawable(GrapherViewModel vm) => _vm = vm;

    private static readonly Color Bg = Color.FromArgb("#0E1626");
    private static readonly Color Grid = Color.FromArgb("#1C2A45");
    private static readonly Color Axis = Color.FromArgb("#4B5D80");
    private static readonly Color LabelCol = Color.FromArgb("#8CA0C4");
    private static readonly Color TraceCol = Color.FromArgb("#FDE68A");

    public void Draw(ICanvas canvas, RectF rect)
    {
        float w = rect.Width, h = rect.Height;
        canvas.FillColor = Bg;
        canvas.FillRectangle(0, 0, w, h);
        if (w < 2 || h < 2) return;

        var vp = _vm.Viewport;
        vp.PixelWidth = w;
        vp.PixelHeight = h;

        DrawGrid(canvas, w, h);
        DrawAxes(canvas, w, h);

        foreach (var fn in _vm.Functions)
            if (fn.IsPlottable)
                DrawCurve(canvas, fn, w, h);

        if (_vm.PlotKind != ViewModels.StatPlotKind.Off)
            DrawStatPlot(canvas, w, h);

        DrawTrace(canvas, w, h);
    }

    private static readonly Color StatCol = Color.FromArgb("#38BDF8");

    private void DrawStatPlot(ICanvas canvas, float w, float h)
    {
        var vp = _vm.Viewport;
        var xs = _vm.StatData[_vm.PlotXList];
        if (xs.Count == 0) return;
        canvas.StrokeColor = StatCol;
        canvas.StrokeSize = 1.6f;
        canvas.FillColor = StatCol;

        switch (_vm.PlotKind)
        {
            case ViewModels.StatPlotKind.Scatter:
            case ViewModels.StatPlotKind.XyLine:
            {
                var ys = _vm.StatData[_vm.PlotYList];
                int n = Math.Min(xs.Count, ys.Count);
                float? pxPrev = null, pyPrev = null;
                for (int i = 0; i < n; i++)
                {
                    float px = (float)vp.ScreenX(xs[i]), py = (float)vp.ScreenY(ys[i]);
                    if (_vm.PlotKind == ViewModels.StatPlotKind.XyLine && pxPrev is float ppx && pyPrev is float ppy)
                        canvas.DrawLine(ppx, ppy, px, py);
                    canvas.FillRectangle(px - 2.5f, py - 2.5f, 5, 5);
                    pxPrev = px; pyPrev = py;
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
                float baseY = (float)vp.ScreenY(0);
                canvas.FillColor = Color.FromRgba(56, 189, 248, 90);
                for (int b = 0; b < bins; b++)
                {
                    float x0 = (float)vp.ScreenX(lo + b * bw), x1 = (float)vp.ScreenX(lo + (b + 1) * bw);
                    float barH = (float)(counts[b] / (double)maxC * (h * 0.6));
                    canvas.FillRectangle(x0, baseY - barH, Math.Max(1, x1 - x0 - 1), barH);
                    canvas.DrawRectangle(x0, baseY - barH, Math.Max(1, x1 - x0 - 1), barH);
                }
                break;
            }
            case ViewModels.StatPlotKind.Box:
            {
                var s = Engine.Stats.OneVar(xs);
                float y = h * 0.5f, box = h * 0.12f;
                float xa = (float)vp.ScreenX(s.Min), xq1 = (float)vp.ScreenX(s.Q1),
                      xm = (float)vp.ScreenX(s.Median), xq3 = (float)vp.ScreenX(s.Q3), xb = (float)vp.ScreenX(s.Max);
                canvas.DrawLine(xa, y, xq1, y);
                canvas.DrawLine(xq3, y, xb, y);
                canvas.DrawLine(xa, y - box / 2, xa, y + box / 2);
                canvas.DrawLine(xb, y - box / 2, xb, y + box / 2);
                canvas.FillColor = Color.FromRgba(56, 189, 248, 60);
                canvas.FillRectangle(xq1, y - box, xq3 - xq1, box * 2);
                canvas.DrawRectangle(xq1, y - box, xq3 - xq1, box * 2);
                canvas.StrokeSize = 2.4f;
                canvas.DrawLine(xm, y - box, xm, y + box);
                break;
            }
        }
    }

    private void DrawGrid(ICanvas canvas, float w, float h)
    {
        var vp = _vm.Viewport;
        double xscl = vp.XScl > 0 ? vp.XScl : 1;
        double yscl = vp.YScl > 0 ? vp.YScl : 1;
        while (vp.SpanX / xscl > 40) xscl *= 2;
        while (vp.SpanY / yscl > 40) yscl *= 2;

        canvas.StrokeColor = Grid;
        canvas.StrokeSize = 1;

        for (double x = Math.Ceiling(vp.XMin / xscl) * xscl; x <= vp.XMax; x += xscl)
        {
            float px = (float)vp.ScreenX(x);
            canvas.DrawLine(px, 0, px, h);
        }
        for (double y = Math.Ceiling(vp.YMin / yscl) * yscl; y <= vp.YMax; y += yscl)
        {
            float py = (float)vp.ScreenY(y);
            canvas.DrawLine(0, py, w, py);
        }
    }

    private void DrawAxes(ICanvas canvas, float w, float h)
    {
        var vp = _vm.Viewport;
        float x0 = (float)vp.ScreenX(0);
        float y0 = (float)vp.ScreenY(0);

        canvas.StrokeColor = Axis;
        canvas.StrokeSize = 1.4f;
        if (x0 >= 0 && x0 <= w) canvas.DrawLine(x0, 0, x0, h);
        if (y0 >= 0 && y0 <= h) canvas.DrawLine(0, y0, w, y0);

        double xscl = vp.XScl > 0 ? vp.XScl : 1;
        double yscl = vp.YScl > 0 ? vp.YScl : 1;
        while (vp.SpanX / xscl > 20) xscl *= 2;
        while (vp.SpanY / yscl > 20) yscl *= 2;

        canvas.FontColor = LabelCol;
        canvas.FontSize = 11;

        float labelY = Math.Clamp(y0 + 3, 2, h - 16);
        for (double x = Math.Ceiling(vp.XMin / xscl) * xscl; x <= vp.XMax; x += xscl)
        {
            if (Math.Abs(x) < xscl / 2) continue;
            canvas.DrawString(Trim(x), (float)vp.ScreenX(x) + 3, labelY, HorizontalAlignment.Left);
        }
        float labelX = Math.Clamp(x0 + 4, 2, w - 44);
        for (double y = Math.Ceiling(vp.YMin / yscl) * yscl; y <= vp.YMax; y += yscl)
        {
            if (Math.Abs(y) < yscl / 2) continue;
            canvas.DrawString(Trim(y), labelX, (float)vp.ScreenY(y) - 12, HorizontalAlignment.Left);
        }
    }

    private void DrawCurve(ICanvas canvas, GraphFunction fn, float w, float h)
    {
        var vp = _vm.Viewport;
        var path = new PathF();
        bool open = false;
        double prevPy = 0;
        double breakGap = h * 3;

        for (float px = 0; px <= w; px += 1f)
        {
            double x = vp.WorldX(px);
            double y = _vm.Evaluate(fn, x);
            if (double.IsNaN(y) || double.IsInfinity(y)) { open = false; continue; }

            double py = vp.ScreenY(y);
            if (!open) { path.MoveTo(px, (float)py); open = true; }
            else if (Math.Abs(py - prevPy) > breakGap) path.MoveTo(px, (float)py);
            else path.LineTo(px, (float)py);
            prevPy = py;
        }

        canvas.StrokeColor = fn.Color;
        canvas.StrokeSize = 2;
        canvas.StrokeLineJoin = LineJoin.Round;
        canvas.DrawPath(path);
    }

    private void DrawTrace(ICanvas canvas, float w, float h)
    {
        if (!_vm.Tracing) return;
        var fn = _vm.TraceFunction;
        if (fn is not { IsPlottable: true }) return;
        var vp = _vm.Viewport;

        double x = _vm.TraceX;
        double y = _vm.Evaluate(fn, x);
        float px = (float)vp.ScreenX(x);

        canvas.StrokeColor = TraceCol;
        canvas.StrokeSize = 1;
        canvas.StrokeDashPattern = new float[] { 4, 4 };
        canvas.DrawLine(px, 0, px, h);

        if (!double.IsNaN(y) && !double.IsInfinity(y))
        {
            float py = (float)vp.ScreenY(y);
            canvas.DrawLine(0, py, w, py);
            canvas.StrokeDashPattern = null;
            canvas.FillColor = TraceCol;
            canvas.FillCircle(px, py, 4);
        }
        canvas.StrokeDashPattern = null;
    }

    private static string Trim(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
}
