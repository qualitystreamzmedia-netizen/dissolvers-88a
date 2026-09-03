using System.Windows;
using Dissolvers88A.Engine;

namespace Dissolvers88A;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StatsView.SendToGraph += SendFitToGraph;
        StatsView.ShowScatter += (x, y) =>
        {
            GraphView.SetStatPlot(ViewModels.StatPlotKind.Scatter, x, y);
            TabGraph.IsChecked = true;
        };
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (CalcView == null || GraphView == null || StatsView == null) return;

        CalcView.Visibility = ReferenceEquals(sender, TabCalc) ? Visibility.Visible : Visibility.Collapsed;
        GraphView.Visibility = ReferenceEquals(sender, TabGraph) ? Visibility.Visible : Visibility.Collapsed;
        StatsView.Visibility = ReferenceEquals(sender, TabStats) ? Visibility.Visible : Visibility.Collapsed;

        if (ReferenceEquals(sender, TabGraph)) GraphView.OnShown();
        else if (ReferenceEquals(sender, TabCalc)) CalcView.FocusInput();
    }

    private void Angle_Checked(object sender, RoutedEventArgs e)
    {
        if (CalcView == null || GraphView == null) return;
        bool deg = ReferenceEquals(sender, AngleDeg);
        CalcView.ViewModel.IsDegrees = deg;
        GraphView.ViewModel.IsDegrees = deg;
    }

    private void SendFitToGraph(string expression)
    {
        GraphView.ViewModel.Functions[0].Text = expression;
        GraphView.ViewModel.Functions[0].Enabled = true;
        TabGraph.IsChecked = true;
    }
}
