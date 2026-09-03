using System.Collections.ObjectModel;
using System.Windows.Media;
using Dissolvers88A.Engine;
using Dissolvers88A.Graphing;
using Dissolvers88A.Mvvm;

namespace Dissolvers88A.ViewModels;

public enum StatPlotKind { Off, Scatter, XyLine, Histogram, Box }

public sealed class GrapherViewModel : ObservableObject
{
    private readonly EvalContext _ctx = new();

    public ObservableCollection<GraphFunction> Functions { get; } = new();
    public Viewport Viewport { get; } = new();

    /// <summary>Raised whenever the graph needs to be redrawn.</summary>
    public event Action? GraphChanged;

    // ---- stat plot (reads the shared L1–L6) ----
    public StatData StatData => AppState.Stats;

    private StatPlotKind _plotKind = StatPlotKind.Off;
    public StatPlotKind PlotKind { get => _plotKind; set { if (Set(ref _plotKind, value)) GraphChanged?.Invoke(); } }

    private int _plotX;
    public int PlotXList { get => _plotX; set { if (Set(ref _plotX, value) && _plotKind != StatPlotKind.Off) GraphChanged?.Invoke(); } }

    private int _plotY = 1;
    public int PlotYList { get => _plotY; set { if (Set(ref _plotY, value) && _plotKind != StatPlotKind.Off) GraphChanged?.Invoke(); } }

    public GrapherViewModel()
    {
        AppState.Stats.Changed += () => { if (_plotKind != StatPlotKind.Off) GraphChanged?.Invoke(); };

        var colors = new[]
        {
            (Color)ColorConverter.ConvertFromString("#2563EB"),
            (Color)ColorConverter.ConvertFromString("#DC2626"),
            (Color)ColorConverter.ConvertFromString("#0F766E"),
            (Color)ColorConverter.ConvertFromString("#7C3AED"),
            (Color)ColorConverter.ConvertFromString("#D97706"),
            (Color)ColorConverter.ConvertFromString("#DB2777"),
        };
        for (int i = 0; i < 6; i++)
        {
            var fn = new GraphFunction($"Y{i + 1}", colors[i]);
            fn.Changed += () => GraphChanged?.Invoke();
            Functions.Add(fn);
        }
        Functions[0].Text = "";
    }

    public AngleMode AngleMode
    {
        get => _ctx.AngleMode;
        set { _ctx.AngleMode = value; Raise(); Raise(nameof(IsDegrees)); GraphChanged?.Invoke(); }
    }

    public bool IsDegrees
    {
        get => _ctx.AngleMode == AngleMode.Degrees;
        set { AngleMode = value ? AngleMode.Degrees : AngleMode.Radians; }
    }

    public bool AnyPlottable => Functions.Any(f => f.IsPlottable);

    public double Evaluate(GraphFunction fn, double x)
    {
        if (fn.Compiled == null) return double.NaN;
        _ctx.Store("X", x);
        try { return Evaluator.Eval(fn.Compiled, _ctx); }
        catch { return double.NaN; }
    }

    // ---- ZOOM commands --------------------------------------------------

    public void ZoomStandard() { Viewport.Standard(); GraphChanged?.Invoke(); }
    public void ZoomIn() { Viewport.ZoomCenter(0.25); GraphChanged?.Invoke(); }
    public void ZoomOut() { Viewport.ZoomCenter(4.0); GraphChanged?.Invoke(); }
    public void ZoomSquare() { Viewport.ZoomSquare(); GraphChanged?.Invoke(); }
    public void ZoomTrig() { Viewport.ZoomTrig(); GraphChanged?.Invoke(); }
    public void ZoomDecimal() { Viewport.ZoomDecimal(); GraphChanged?.Invoke(); }

    public void ZoomFit()
    {
        var ys = new List<double>();
        int n = Math.Max(64, (int)Viewport.PixelWidth);
        foreach (var f in Functions.Where(f => f.IsPlottable))
            for (int i = 0; i <= n; i++)
                ys.Add(Evaluate(f, Viewport.XMin + (double)i / n * Viewport.SpanX));
        Viewport.FitY(ys);
        GraphChanged?.Invoke();
    }

    /// <summary>ZoomStat — scale the window to the min/max of the stat plot's lists.</summary>
    public void ZoomStat()
    {
        if (_plotKind == StatPlotKind.Off) return;
        var xs = StatData[PlotXList];
        if (xs.Count == 0) return;

        if (_plotKind is StatPlotKind.Scatter or StatPlotKind.XyLine)
        {
            var ys = StatData[PlotYList];
            int n = Math.Min(xs.Count, ys.Count);
            if (n == 0) return;
            Viewport.FitToData(xs.Take(n).ToList(), ys.Take(n).ToList());
        }
        else   // Histogram / Box — one-variable, frame the X data only
        {
            Viewport.FitToData(xs, Array.Empty<double>());
        }
        GraphChanged?.Invoke();
    }

    public void NotifyWindowEdited() => GraphChanged?.Invoke();

    // ---- TRACE --------------------------------------------------------

    public bool Tracing { get; private set; }
    public int TraceIndex { get; private set; }
    public double TraceX { get; private set; }

    public GraphFunction? TraceFunction =>
        Tracing && TraceIndex >= 0 && TraceIndex < Functions.Count ? Functions[TraceIndex] : null;

    public void StartTrace()
    {
        var first = Functions.ToList().FindIndex(f => f.IsPlottable);
        if (first < 0) return;
        Tracing = true;
        TraceIndex = first;
        TraceX = (Viewport.XMin + Viewport.XMax) / 2;
        GraphChanged?.Invoke();
    }

    public void StopTrace()
    {
        Tracing = false;
        GraphChanged?.Invoke();
    }

    public void TraceStep(int dir)
    {
        if (!Tracing) return;
        double step = Viewport.SpanX / Math.Max(1, Viewport.PixelWidth) * 2;
        TraceX = Math.Clamp(TraceX + dir * step, Viewport.XMin, Viewport.XMax);
        GraphChanged?.Invoke();
    }

    public void TraceSwitchFunction(int dir)
    {
        if (!Tracing) return;
        var plottable = Enumerable.Range(0, Functions.Count).Where(i => Functions[i].IsPlottable).ToList();
        if (plottable.Count == 0) return;
        int pos = plottable.IndexOf(TraceIndex);
        pos = (pos + dir + plottable.Count) % plottable.Count;
        TraceIndex = plottable[pos];
        GraphChanged?.Invoke();
    }
}
