using System.Globalization;
using System.Text;
using Dissolvers88A.Engine;

namespace Dissolvers88A.Maui.Views;

public partial class StatsView : ContentView
{
    private readonly StatData _data = AppState.Stats;
    private int _editing;
    private bool _loading;
    private string _lastFit = "";

    /// <summary>Raised when the user sends a regression fit to the grapher.</summary>
    public event Action<string>? SendToGraph;

    /// <summary>Raised on 2-Var Stats — (xListIndex, yListIndex) to show as a scatter plot.</summary>
    public event Action<int, int>? ShowScatter;

    public StatsView()
    {
        InitializeComponent();

        var names = Enumerable.Range(1, 6).Select(i => "L" + i).ToArray();
        ListPicker.ItemsSource = names;
        XListPicker.ItemsSource = names;
        YListPicker.ItemsSource = names;
        ListPicker.SelectedIndex = 0;
        XListPicker.SelectedIndex = 0;
        YListPicker.SelectedIndex = 1;
    }

    private void OnListPicked(object? sender, EventArgs e)
    {
        _editing = Math.Max(0, ListPicker.SelectedIndex);
        _loading = true;
        DataEditor.Text = string.Join("\n", _data[_editing].Select(v => v.ToString("0.######", CultureInfo.InvariantCulture)));
        _loading = false;
    }

    private void OnDataChanged(object? sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        var values = new List<double>();
        foreach (var tok in (e.NewTextValue ?? "").Split(new[] { ' ', '\t', '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            if (double.TryParse(tok, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                values.Add(d);
        _data.Set(_editing, values);
    }

    private void OnClearAll(object? sender, EventArgs e)
    {
        _data.ClearAll();
        _loading = true; DataEditor.Text = ""; _loading = false;
        Results.Text = "Cleared.";
        SendY1.IsEnabled = false;
    }

    // ---- calculations ------------------------------------------------

    private void OnOneVar(object? sender, EventArgs e)
    {
        try
        {
            var s = Stats.OneVar(_data[_editing]);
            var b = new StringBuilder();
            b.AppendLine($"1-Var Stats  (L{_editing + 1})").AppendLine();
            b.AppendLine($"  mean = {N(s.Mean)}");
            b.AppendLine($"  Sum x   = {N(s.Sum)}");
            b.AppendLine($"  Sum x^2 = {N(s.SumSq)}");
            b.AppendLine($"  Sx   = {N(s.SampleStdDev)}  (sample)");
            b.AppendLine($"  sigx = {N(s.PopStdDev)}  (pop.)");
            b.AppendLine($"  n    = {s.N}").AppendLine();
            b.AppendLine($"  minX = {N(s.Min)}");
            b.AppendLine($"  Q1   = {N(s.Q1)}");
            b.AppendLine($"  Med  = {N(s.Median)}");
            b.AppendLine($"  Q3   = {N(s.Q3)}");
            b.Append($"  maxX = {N(s.Max)}");
            Results.Text = b.ToString();
        }
        catch (CalcException ex) { Results.Text = ex.Message; }
    }

    private void OnTwoVar(object? sender, EventArgs e)
    {
        try
        {
            int xi = XListPicker.SelectedIndex, yi = YListPicker.SelectedIndex;
            var s = Stats.TwoVar(_data[xi], _data[yi]);
            var b = new StringBuilder();
            b.AppendLine("2-Var Stats").AppendLine();
            b.AppendLine($"  mean x = {N(s.MeanX)}   mean y = {N(s.MeanY)}");
            b.AppendLine($"  Sum x  = {N(s.SumX)}   Sum y  = {N(s.SumY)}");
            b.AppendLine($"  Sum xy = {N(s.SumXY)}");
            b.AppendLine($"  Sx = {N(s.SampleStdDevX)}   Sy = {N(s.SampleStdDevY)}");
            b.Append($"  n  = {s.N}");
            Results.Text = b.ToString();
            ShowScatter?.Invoke(xi, yi);
        }
        catch (CalcException ex) { Results.Text = ex.Message; }
    }

    private void OnLinReg(object? sender, EventArgs e)
    {
        try
        {
            var s = Stats.LinReg(_data[XListPicker.SelectedIndex], _data[YListPicker.SelectedIndex]);
            _lastFit = $"{Calculator.Format(s.A)}*X+({Calculator.Format(s.B)})";
            var b = new StringBuilder();
            b.AppendLine("LinReg   ŷ = a·x + b").AppendLine();
            b.AppendLine($"  a  = {N(s.A)}  (slope)");
            b.AppendLine($"  b  = {N(s.B)}  (intercept)");
            b.AppendLine($"  r  = {N(s.R)}");
            b.Append($"  r² = {N(s.R2)}");
            Results.Text = b.ToString();
            SendY1.IsEnabled = true;
        }
        catch (CalcException ex) { Results.Text = ex.Message; SendY1.IsEnabled = false; }
    }

    private void OnSendY1(object? sender, EventArgs e)
    {
        if (_lastFit.Length > 0) SendToGraph?.Invoke(_lastFit);
    }

    private static string N(double v) => Calculator.Format(v);
}
