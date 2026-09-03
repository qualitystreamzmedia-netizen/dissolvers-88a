using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;
using Dissolvers88A.ViewModels;

namespace Dissolvers88A.Views;

public partial class GrapherView : UserControl
{
    public GrapherViewModel ViewModel { get; } = new();

    private bool _syncingWindow;

    public GrapherView()
    {
        InitializeComponent();
        DataContext = ViewModel;

        FunctionList.ItemsSource = ViewModel.Functions;
        Surface.Grapher = ViewModel;
        Surface.ViewportChanged += PushWindowToBoxes;

        foreach (var box in new[] { XminBox, XmaxBox, YminBox, YmaxBox, XsclBox, YsclBox })
        {
            box.LostKeyboardFocus += (_, _) => PullWindowFromBoxes();
            box.KeyDown += (_, e) => { if (e.Key is Key.Enter or Key.Return) PullWindowFromBoxes(); };
        }

        var lists = Enumerable.Range(1, 6).Select(i => "L" + i).ToArray();
        PlotXBox.ItemsSource = lists;
        PlotYBox.ItemsSource = lists;
        PlotXBox.SelectedIndex = 0;
        PlotYBox.SelectedIndex = 1;

        Loaded += (_, _) => PushWindowToBoxes();
        ViewModel.Functions[0].Text = "";
    }

    private void Plot_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (PlotKindBox == null || PlotXBox == null || PlotYBox == null) return;
        ViewModel.PlotKind = (StatPlotKind)PlotKindBox.SelectedIndex;
        ViewModel.PlotXList = Math.Max(0, PlotXBox.SelectedIndex);
        ViewModel.PlotYList = Math.Max(0, PlotYBox.SelectedIndex);
    }

    /// <summary>Turn on a stat plot from elsewhere (e.g. 2-Var Stats → scatter).</summary>
    public void SetStatPlot(StatPlotKind kind, int xList, int yList)
    {
        PlotXBox.SelectedIndex = xList;
        PlotYBox.SelectedIndex = yList;
        PlotKindBox.SelectedIndex = (int)kind;   // fires Plot_Changed
    }

    public void OnShown()
    {
        PushWindowToBoxes();
        Surface.InvalidateVisual();
        Surface.Focus();
    }

    // ---- WINDOW fields <-> viewport -----------------------------------

    private void PushWindowToBoxes()
    {
        _syncingWindow = true;
        var v = ViewModel.Viewport;
        XminBox.Text = Fmt(v.XMin); XmaxBox.Text = Fmt(v.XMax);
        YminBox.Text = Fmt(v.YMin); YmaxBox.Text = Fmt(v.YMax);
        XsclBox.Text = Fmt(v.XScl); YsclBox.Text = Fmt(v.YScl);
        _syncingWindow = false;
    }

    private void PullWindowFromBoxes()
    {
        if (_syncingWindow) return;
        var v = ViewModel.Viewport;
        v.XMin = Parse(XminBox.Text, v.XMin);
        v.XMax = Parse(XmaxBox.Text, v.XMax);
        v.YMin = Parse(YminBox.Text, v.YMin);
        v.YMax = Parse(YmaxBox.Text, v.YMax);
        v.XScl = Parse(XsclBox.Text, v.XScl);
        v.YScl = Parse(YsclBox.Text, v.YScl);
        if (v.XMax <= v.XMin) v.XMax = v.XMin + 1;
        if (v.YMax <= v.YMin) v.YMax = v.YMin + 1;
        ViewModel.NotifyWindowEdited();
    }

    private static string Fmt(double d) => d.ToString("0.######", CultureInfo.InvariantCulture);

    private static double Parse(string s, double fallback)
        => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : fallback;

    // ---- zoom --------------------------------------------------------

    private void Zoom_Standard(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomStandard(); PushWindowToBoxes(); }
    private void Zoom_Fit(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomFit(); PushWindowToBoxes(); }
    private void Zoom_Square(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomSquare(); PushWindowToBoxes(); }
    private void Zoom_In(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomIn(); PushWindowToBoxes(); }
    private void Zoom_Out(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomOut(); PushWindowToBoxes(); }
    private void Zoom_Trig(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomTrig(); PushWindowToBoxes(); }
    private void Zoom_Decimal(object s, System.Windows.RoutedEventArgs e) { ViewModel.ZoomDecimal(); PushWindowToBoxes(); }

    private void Trace_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (TraceToggle.IsChecked == true) ViewModel.StartTrace();
        else ViewModel.StopTrace();
        Surface.Focus();
    }
}
