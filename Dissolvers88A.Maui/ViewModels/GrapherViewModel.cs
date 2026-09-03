using System.Collections.ObjectModel;
using Dissolvers88A.Engine;
using Dissolvers88A.Maui.Graphing;
using Dissolvers88A.Maui.Mvvm;

namespace Dissolvers88A.Maui.ViewModels;

public enum StatPlotKind { Off, Scatter, XyLine, Histogram, Box }

public sealed class GrapherViewModel : ObservableObject
{
    private readonly EvalContext _ctx = new();

    public ObservableCollection<GraphFunction> Functions { get; } = new();
    public Viewport Viewport { get; } = new();

    /// <summary>Raised whenever the graph needs a redraw.</summary>
    public event Action? GraphChanged;

    // ---- stat plot (reads the shared L1–L6) ----
    public Engine.StatData StatData => AppState.Stats;

    private StatPlotKind _plotKind = StatPlotKind.Off;
    public StatPlotKind PlotKind { get => _plotKind; set { if (Set(ref _plotKind, value)) GraphChanged?.Invoke(); } }

    private int _plotX;
    public int PlotXList { get => _plotX; set { if (Set(ref _plotX, value) && _plotKind != StatPlotKind.Off) GraphChanged?.Invoke(); } }

    private int _plotY = 1;
    public int PlotYList { get => _plotY; set { if (Set(ref _plotY, value) && _plotKind != StatPlotKind.Off) GraphChanged?.Invoke(); } }

    public GrapherViewModel()
    {
        AppState.Stats.Changed += () => { if (_plotKind != StatPlotKind.Off) GraphChanged?.Invoke(); };

        var colours = new[]
        {
            Color.FromArgb("#2563EB"), Color.FromArgb("#DC2626"), Color.FromArgb("#0F766E"),
            Color.FromArgb("#7C3AED"), Color.FromArgb("#D97706"), Color.FromArgb("#DB2777"),
        };
        for (int i = 0; i < 6; i++)
        {
            var fn = new GraphFunction($"Y{i + 1}", colours[i]);
            fn.Changed += () => GraphChanged?.Invoke();
            Functions.Add(fn);
        }
    }

    public bool IsDegrees
    {
        get => _ctx.AngleMode == AngleMode.Degrees;
        set
        {
            _ctx.AngleMode = value ? AngleMode.Degrees : AngleMode.Radians;
            Raise();
            Raise(nameof(AngleLabel));
            GraphChanged?.Invoke();
        }
    }

    public string AngleLabel => IsDegrees ? "DEG" : "RAD";

    public void ToggleAngle() => IsDegrees = !IsDegrees;

    public double Evaluate(GraphFunction fn, double x)
    {
        if (fn.Compiled == null) return double.NaN;
        _ctx.Store("X", x);
        try { return Evaluator.Eval(fn.Compiled, _ctx); }
        catch { return double.NaN; }
    }

    // ---- ZOOM ---------------------------------------------------------

    public void ZoomStandard() { Viewport.Standard(); Redraw(); }
    public void ZoomIn() { Viewport.ZoomCenter(0.25); Redraw(); }
    public void ZoomOut() { Viewport.ZoomCenter(4.0); Redraw(); }
    public void ZoomSquare() { Viewport.ZoomSquare(); Redraw(); }
    public void ZoomTrig() { Viewport.ZoomTrig(); Redraw(); }
    public void ZoomDecimal() { Viewport.ZoomDecimal(); Redraw(); }

    public void ZoomFit()
    {
        var ys = new List<double>();
        int n = Math.Max(64, (int)Viewport.PixelWidth);
        foreach (var f in Functions.Where(f => f.IsPlottable))
            for (int i = 0; i <= n; i++)
                ys.Add(Evaluate(f, Viewport.XMin + (double)i / n * Viewport.SpanX));
        Viewport.FitY(ys);
        Redraw();
    }

    public void Redraw() => GraphChanged?.Invoke();

    // ---- TRACE ------------------------------------------------------

    public bool Tracing { get; private set; }
    public int TraceIndex { get; private set; }
    public double TraceX { get; private set; }

    public GraphFunction? TraceFunction =>
        Tracing && TraceIndex >= 0 && TraceIndex < Functions.Count ? Functions[TraceIndex] : null;

    private string _traceReadout = "";
    public string TraceReadout { get => _traceReadout; private set => Set(ref _traceReadout, value); }

    public bool ToggleTrace()
    {
        if (Tracing) { Tracing = false; TraceReadout = ""; Redraw(); return false; }
        var first = Functions.ToList().FindIndex(f => f.IsPlottable);
        if (first < 0) { TraceReadout = "Nothing to trace"; return false; }
        Tracing = true;
        TraceIndex = first;
        TraceX = (Viewport.XMin + Viewport.XMax) / 2;
        UpdateTraceReadout();
        Redraw();
        return true;
    }

    public void TraceStep(int dir)
    {
        if (!Tracing) return;
        double step = Viewport.SpanX / 94.0;   // ~one TI screen step
        TraceX = Math.Clamp(TraceX + dir * step, Viewport.XMin, Viewport.XMax);
        UpdateTraceReadout();
        Redraw();
    }

    /// <summary>Move the trace cursor by a signed distance in world-X (drag on the graph).</summary>
    public void TraceStepBy(double deltaWorldX)
    {
        if (!Tracing) return;
        TraceX = Math.Clamp(TraceX + deltaWorldX, Viewport.XMin, Viewport.XMax);
        UpdateTraceReadout();
        Redraw();
    }

    public void TraceSwitchFunction(int dir)
    {
        if (!Tracing) return;
        var plottable = Enumerable.Range(0, Functions.Count).Where(i => Functions[i].IsPlottable).ToList();
        if (plottable.Count == 0) return;
        int pos = plottable.IndexOf(TraceIndex);
        pos = (pos + dir + plottable.Count) % plottable.Count;
        TraceIndex = plottable[pos];
        UpdateTraceReadout();
        Redraw();
    }

    private void UpdateTraceReadout()
    {
        var f = TraceFunction;
        if (f == null) { TraceReadout = ""; return; }
        double y = Evaluate(f, TraceX);
        TraceReadout = $"{f.Label}:  X = {Calculator.Format(TraceX)}   Y = {Calculator.Format(y)}";
    }
}
