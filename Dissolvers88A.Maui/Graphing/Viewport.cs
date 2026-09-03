namespace Dissolvers88A.Maui.Graphing;

/// <summary>
/// The visible window of the coordinate plane and the world↔screen maths.
/// Identical to the WPF app's Viewport — pure geometry, no UI dependency.
/// </summary>
public sealed class Viewport
{
    public double XMin = -10, XMax = 10, YMin = -10, YMax = 10;
    public double XScl = 1, YScl = 1;

    public double PixelWidth = 1, PixelHeight = 1;

    public double SpanX => XMax - XMin;
    public double SpanY => YMax - YMin;

    public double ScreenX(double x) => (x - XMin) / SpanX * PixelWidth;
    public double ScreenY(double y) => (YMax - y) / SpanY * PixelHeight;
    public double WorldX(double px) => XMin + px / PixelWidth * SpanX;
    public double WorldY(double py) => YMax - py / PixelHeight * SpanY;

    public void PanByPixels(double dxPixels, double dyPixels)
    {
        double wx = dxPixels / PixelWidth * SpanX;
        double wy = dyPixels / PixelHeight * SpanY;
        XMin -= wx; XMax -= wx;
        YMin += wy; YMax += wy;
    }

    public void ZoomAt(double px, double py, double factor)
    {
        double cx = WorldX(px), cy = WorldY(py);
        XMin = cx + (XMin - cx) * factor; XMax = cx + (XMax - cx) * factor;
        YMin = cy + (YMin - cy) * factor; YMax = cy + (YMax - cy) * factor;
    }

    public void ZoomCenter(double factor) => ZoomAt(PixelWidth / 2, PixelHeight / 2, factor);

    // ---- ZOOM menu presets ------------------------------------------------

    public void Standard() { XMin = -10; XMax = 10; YMin = -10; YMax = 10; XScl = 1; YScl = 1; }

    public void ZoomTrig()
    {
        XMin = -6.283185307; XMax = 6.283185307; XScl = Math.PI / 2;
        YMin = -4; YMax = 4; YScl = 1;
    }

    public void ZoomDecimal()
    {
        double w = PixelWidth > 1 ? PixelWidth : 640;
        double h = PixelHeight > 1 ? PixelHeight : 420;
        XMin = -w * 0.0125; XMax = w * 0.0125;
        YMin = -h * 0.0125; YMax = h * 0.0125;
        XScl = 1; YScl = 1;
    }

    /// <summary>Make one screen pixel the same distance on both axes.</summary>
    public void ZoomSquare()
    {
        if (PixelWidth <= 0 || PixelHeight <= 0) return;
        double unitsPerPxX = SpanX / PixelWidth;
        double newSpanY = unitsPerPxX * PixelHeight;
        double cy = (YMin + YMax) / 2;
        YMin = cy - newSpanY / 2;
        YMax = cy + newSpanY / 2;
    }

    public void FitY(IReadOnlyList<double> sampledYs)
    {
        double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
        foreach (var y in sampledYs)
            if (!double.IsNaN(y) && !double.IsInfinity(y)) { lo = Math.Min(lo, y); hi = Math.Max(hi, y); }
        if (double.IsInfinity(lo) || lo == hi) return;
        double pad = (hi - lo) * 0.1;
        YMin = lo - pad; YMax = hi + pad;
    }
}
