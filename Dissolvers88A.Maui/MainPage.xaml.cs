namespace Dissolvers88A.Maui;

public partial class MainPage : ContentPage
{
    private bool _degrees;

    public MainPage()
    {
        InitializeComponent();
        StatsView.SendToGraph += SendFitToGraph;
        StatsView.ShowScatter += (x, y) =>
        {
            GraphView.SetStatPlot(Dissolvers88A.Maui.ViewModels.StatPlotKind.Scatter, x, y);
            Show(1);
        };
    }

    private void OnCalcTab(object? sender, EventArgs e) => Show(0);
    private void OnGraphTab(object? sender, EventArgs e) => Show(1);
    private void OnStatsTab(object? sender, EventArgs e) => Show(2);
    private void OnRTab(object? sender, EventArgs e) => Show(3);

    private void Show(int mode)
    {
        CalcView.IsVisible = mode == 0;
        GraphView.IsVisible = mode == 1;
        StatsView.IsVisible = mode == 2;
        RView.IsVisible = mode == 3;

        Style(CalcTab, mode == 0);
        Style(GraphTab, mode == 1);
        Style(StatsTab, mode == 2);
        Style(RTab, mode == 3);

        if (mode == 1) GraphView.OnShown();
        else if (mode == 3) RView.OnShown();
    }

    private static void Style(Button tab, bool active)
    {
        tab.BackgroundColor = active ? Color.FromArgb("#0F172A") : Colors.Transparent;
        tab.TextColor = active ? Colors.White : Color.FromArgb("#334155");
    }

    private void OnAngle(object? sender, EventArgs e)
    {
        _degrees = !_degrees;
        AngleButton.Text = _degrees ? "DEG" : "RAD";
        CalcView.SetDegrees(_degrees);
        GraphView.SetDegrees(_degrees);
    }

    private void SendFitToGraph(string expression)
    {
        GraphView.Vm.Functions[0].Text = expression;
        GraphView.Vm.Functions[0].Enabled = true;
        Show(1);
    }
}
