using System.Globalization;
using Dissolvers88A.Maui.Graphing;
using Dissolvers88A.Maui.ViewModels;

namespace Dissolvers88A.Maui.Views;

public partial class GrapherView : ContentView
{
    public GrapherViewModel Vm { get; } = new();
    private bool _syncingWindow;
    private double _panLastX, _panLastY;

    public GrapherView()
    {
        InitializeComponent();
        BindingContext = Vm;

        Surface.Drawable = new GraphDrawable(Vm);
        Vm.GraphChanged += () => MainThread.BeginInvokeOnMainThread(Surface.Invalidate);

        foreach (var box in new[] { XminBox, XmaxBox, YminBox, YmaxBox, XsclBox, YsclBox })
        {
            box.Completed += (_, _) => PullWindow();
            box.Unfocused += (_, _) => PullWindow();
        }

        var lists = Enumerable.Range(1, 6).Select(i => "L" + i).ToArray();
        PlotXPicker.ItemsSource = lists;
        PlotYPicker.ItemsSource = lists;
        PlotXPicker.SelectedIndex = 0;
        PlotYPicker.SelectedIndex = 1;
        PlotKindPicker.SelectedIndex = 0;

        Loaded += (_, _) => PushWindow();
    }

    private void OnPlotChanged(object? sender, EventArgs e)
    {
        if (PlotKindPicker == null || PlotXPicker == null || PlotYPicker == null) return;
        var newKind = (StatPlotKind)Math.Max(0, PlotKindPicker.SelectedIndex);
        bool turnedOn = Vm.PlotKind == StatPlotKind.Off && newKind != StatPlotKind.Off;

        Vm.PlotKind = newKind;
        Vm.PlotXList = Math.Max(0, PlotXPicker.SelectedIndex);
        Vm.PlotYList = Math.Max(0, PlotYPicker.SelectedIndex);

        if (turnedOn) { Vm.ZoomStat(); PushWindow(); }
    }

    /// <summary>Turn on a stat plot from elsewhere (e.g. 2-Var Stats → scatter).</summary>
    public void SetStatPlot(StatPlotKind kind, int xList, int yList)
    {
        PlotXPicker.SelectedIndex = xList;
        PlotYPicker.SelectedIndex = yList;
        PlotKindPicker.SelectedIndex = (int)kind;   // fires OnPlotChanged
        Vm.ZoomStat();                              // frame the data even if the plot was already on
        PushWindow();
    }

    public void SetDegrees(bool deg) => Vm.IsDegrees = deg;

    public void OnShown()
    {
        PushWindow();
        Surface.Invalidate();
    }

    // ---- WINDOW <-> viewport --------------------------------------

    private void PushWindow()
    {
        _syncingWindow = true;
        var v = Vm.Viewport;
        XminBox.Text = F(v.XMin); XmaxBox.Text = F(v.XMax);
        YminBox.Text = F(v.YMin); YmaxBox.Text = F(v.YMax);
        XsclBox.Text = F(v.XScl); YsclBox.Text = F(v.YScl);
        _syncingWindow = false;
    }

    private void PullWindow()
    {
        if (_syncingWindow) return;
        var v = Vm.Viewport;
        v.XMin = P(XminBox.Text, v.XMin); v.XMax = P(XmaxBox.Text, v.XMax);
        v.YMin = P(YminBox.Text, v.YMin); v.YMax = P(YmaxBox.Text, v.YMax);
        v.XScl = P(XsclBox.Text, v.XScl); v.YScl = P(YsclBox.Text, v.YScl);
        if (v.XMax <= v.XMin) v.XMax = v.XMin + 1;
        if (v.YMax <= v.YMin) v.YMax = v.YMin + 1;
        Vm.Redraw();
    }

    private static string F(double d) => d.ToString("0.######", CultureInfo.InvariantCulture);
    private static double P(string? s, double fb) =>
        double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fb;

    // ---- zoom ---------------------------------------------------

    private void Zoom_Standard(object s, EventArgs e) { Vm.ZoomStandard(); PushWindow(); }
    private void Zoom_Fit(object s, EventArgs e) { Vm.ZoomFit(); PushWindow(); }
    private void Zoom_Square(object s, EventArgs e) { Vm.ZoomSquare(); PushWindow(); }
    private void Zoom_In(object s, EventArgs e) { Vm.ZoomIn(); PushWindow(); }
    private void Zoom_Out(object s, EventArgs e) { Vm.ZoomOut(); PushWindow(); }
    private void Zoom_Trig(object s, EventArgs e) { Vm.ZoomTrig(); PushWindow(); }
    private void Zoom_Decimal(object s, EventArgs e) { Vm.ZoomDecimal(); PushWindow(); }
    private void Zoom_Stat(object s, EventArgs e) { Vm.ZoomStat(); PushWindow(); }

    private void OnTrace(object? sender, EventArgs e)
    {
        bool on = Vm.ToggleTrace();
        TraceButton.Text = on ? "TRACE ON  ·  drag to move" : "TRACE  ·  drag to move";
        TraceButton.BackgroundColor = on ? Color.FromArgb("#DBEAFE") : Color.FromArgb("#EDF5FF");
    }

    // ---- touch: pan / trace / pinch -----------------------------

    private void OnPan(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _panLastX = 0; _panLastY = 0;
                break;

            case GestureStatus.Running:
                double dx = e.TotalX - _panLastX;
                double dy = e.TotalY - _panLastY;
                _panLastX = e.TotalX; _panLastY = e.TotalY;

                if (Vm.Tracing)
                {
                    double perPx = Vm.Viewport.SpanX / Math.Max(1, Vm.Viewport.PixelWidth);
                    Vm.TraceStepBy(dx * perPx);
                }
                else
                {
                    Vm.Viewport.PanByPixels(dx, dy);
                    Vm.Redraw();
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (!Vm.Tracing) PushWindow();
                break;
        }
    }

    private void OnPinch(object? sender, PinchGestureUpdatedEventArgs e)
    {
        if (e.Status != GestureStatus.Running || e.Scale <= 0) return;
        var v = Vm.Viewport;
        v.ZoomAt(e.ScaleOrigin.X * v.PixelWidth, e.ScaleOrigin.Y * v.PixelHeight, 1.0 / e.Scale);
        Vm.Redraw();
        PushWindow();
    }
}
