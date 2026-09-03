using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Dissolvers88A.Engine;
using Dissolvers88A.ViewModels;

namespace Dissolvers88A.Views;

public partial class StatsView : UserControl
{
    private readonly ObservableCollection<StatRow> _rows = new();
    private readonly StatData _data = AppState.Stats;

    /// <summary>Raised when the user sends a regression fit to the grapher.</summary>
    public event Action<string>? SendToGraph;

    /// <summary>Raised on 2-Var Stats — (xListIndex, yListIndex) to show as a scatter plot.</summary>
    public event Action<int, int>? ShowScatter;

    public StatsView()
    {
        InitializeComponent();

        var names = Enumerable.Range(1, 6).Select(i => "L" + i).ToArray();
        OneVarList.ItemsSource = names;
        XList.ItemsSource = names;
        YList.ItemsSource = names;

        Grid.ItemsSource = _rows;
        LoadFromData();
    }

    private void LoadFromData()
    {
        _rows.Clear();
        int max = Enumerable.Range(0, 6).Max(i => _data[i].Count);
        for (int r = 0; r < max; r++)
        {
            var row = new StatRow();
            if (r < _data[0].Count) row.C1 = _data[0][r];
            if (r < _data[1].Count) row.C2 = _data[1][r];
            if (r < _data[2].Count) row.C3 = _data[2][r];
            if (r < _data[3].Count) row.C4 = _data[3][r];
            if (r < _data[4].Count) row.C5 = _data[4][r];
            if (r < _data[5].Count) row.C6 = _data[5][r];
            _rows.Add(row);
        }
    }

    private void Grid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        => Dispatcher.BeginInvoke(RebuildLists);

    private void RebuildLists()
    {
        for (int col = 0; col < 6; col++)
        {
            int c = col;
            _data.Set(c, _rows.Where(r => r[c].HasValue).Select(r => r[c]!.Value));
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        _data.ClearAll();
        _rows.Clear();
        Results.Text = "Cleared.";
        SendY1.IsEnabled = false;
    }

    // ---- calculations ----------------------------------------------

    private void OneVar_Click(object sender, RoutedEventArgs e)
    {
        RebuildLists();
        int li = OneVarList.SelectedIndex;
        try
        {
            var s = Stats.OneVar(_data[li]);
            Results.Text = FormatOneVar("L" + (li + 1), s);
        }
        catch (CalcException ex) { Results.Text = ex.Message; }
    }

    private void TwoVar_Click(object sender, RoutedEventArgs e)
    {
        RebuildLists();
        try
        {
            int xi = XList.SelectedIndex, yi = YList.SelectedIndex;
            var s = Stats.TwoVar(_data[xi], _data[yi]);
            Results.Text = FormatTwoVar(s);
            ShowScatter?.Invoke(xi, yi);
        }
        catch (CalcException ex) { Results.Text = ex.Message; }
    }

    private string _lastFit = "";

    private void LinReg_Click(object sender, RoutedEventArgs e)
    {
        RebuildLists();
        try
        {
            var s = Stats.LinReg(_data[XList.SelectedIndex], _data[YList.SelectedIndex]);
            _lastFit = $"{Calculator.Format(s.A)}*X+({Calculator.Format(s.B)})";
            Results.Text = FormatLinReg(s);
            SendY1.IsEnabled = true;
        }
        catch (CalcException ex) { Results.Text = ex.Message; SendY1.IsEnabled = false; }
    }

    private void SendY1_Click(object sender, RoutedEventArgs e)
    {
        if (_lastFit.Length > 0) SendToGraph?.Invoke(_lastFit);
    }

    // ---- formatting (TI-style) -----------------------------------

    private static string N(double v) => Calculator.Format(v);

    private static string FormatOneVar(string list, OneVarResult s)
    {
        var b = new StringBuilder();
        b.AppendLine($"1-Var Stats  ({list})").AppendLine();
        b.AppendLine($"  mean = {N(s.Mean)}");
        b.AppendLine($"  Sum x   = {N(s.Sum)}");
        b.AppendLine($"  Sum x^2 = {N(s.SumSq)}");
        b.AppendLine($"  Sx   = {N(s.SampleStdDev)}   (sample SD)");
        b.AppendLine($"  sigx = {N(s.PopStdDev)}   (population SD)");
        b.AppendLine($"  n    = {s.N}").AppendLine();
        b.AppendLine($"  minX = {N(s.Min)}");
        b.AppendLine($"  Q1   = {N(s.Q1)}");
        b.AppendLine($"  Med  = {N(s.Median)}");
        b.AppendLine($"  Q3   = {N(s.Q3)}");
        b.Append($"  maxX = {N(s.Max)}");
        return b.ToString();
    }

    private static string FormatTwoVar(TwoVarResult s)
    {
        var b = new StringBuilder();
        b.AppendLine("2-Var Stats").AppendLine();
        b.AppendLine($"  mean x = {N(s.MeanX)}     mean y = {N(s.MeanY)}");
        b.AppendLine($"  Sum x  = {N(s.SumX)}     Sum y  = {N(s.SumY)}");
        b.AppendLine($"  Sum x^2 = {N(s.SumXSq)}     Sum y^2 = {N(s.SumYSq)}");
        b.AppendLine($"  Sum xy  = {N(s.SumXY)}");
        b.AppendLine($"  Sx  = {N(s.SampleStdDevX)}     Sy  = {N(s.SampleStdDevY)}");
        b.AppendLine($"  sigx = {N(s.PopStdDevX)}     sigy = {N(s.PopStdDevY)}");
        b.Append($"  n   = {s.N}");
        return b.ToString();
    }

    private static string FormatLinReg(LinRegResult s)
    {
        var b = new StringBuilder();
        b.AppendLine("LinReg   ŷ = a·x + b").AppendLine();
        b.AppendLine($"  a  = {N(s.A)}   (slope)");
        b.AppendLine($"  b  = {N(s.B)}   (intercept)");
        b.AppendLine($"  r  = {N(s.R)}");
        b.Append($"  r² = {N(s.R2)}");
        return b.ToString();
    }
}
