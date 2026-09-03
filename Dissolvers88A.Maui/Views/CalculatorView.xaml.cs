using Dissolvers88A.Maui.ViewModels;

namespace Dissolvers88A.Maui.Views;

public partial class CalculatorView : ContentView
{
    public CalculatorViewModel Vm { get; } = new();
    private bool _second;

    private static readonly Dictionary<string, string> Secondary = new()
    {
        ["sin("] = "asin(",
        ["cos("] = "acos(",
        ["tan("] = "atan(",
        ["sqrt("] = "cbrt(",
        ["^"] = "nPr(",
        ["^2"] = "^3",
        ["^-1"] = "abs(",
        ["!"] = "nCr(",
        ["ln("] = "e^(",
        ["log("] = "10^(",
    };

    public CalculatorView()
    {
        InitializeComponent();
        BindingContext = Vm;
    }

    public void SetDegrees(bool deg) => Vm.IsDegrees = deg;

    // ---- keypad -----------------------------------------------------

    private void OnSecond(object? sender, EventArgs e)
    {
        _second = !_second;
        SecondKey.BackgroundColor = _second ? Color.FromArgb("#D97706") : Color.FromArgb("#FEF3C7");
        SecondKey.TextColor = _second ? Colors.White : Color.FromArgb("#D97706");
        SetSecondLabels(_second);
    }

    private void SetSecondLabels(bool on)
    {
        KeySin.Text = on ? "sin⁻¹" : "sin";
        KeyCos.Text = on ? "cos⁻¹" : "cos";
        KeyTan.Text = on ? "tan⁻¹" : "tan";
        KeyPow.Text = on ? "nPr" : "^";
        KeyRoot.Text = on ? "∛" : "√";
        KeySq.Text = on ? "x³" : "x²";
        KeyInv.Text = on ? "|x|" : "x⁻¹";
        KeyBang.Text = on ? "nCr" : "!";
        KeyLn.Text = on ? "eˣ" : "ln";
        KeyLog.Text = on ? "10ˣ" : "log";
    }

    private void OnKey(object? sender, EventArgs e)
    {
        if (sender is not Button b || b.CommandParameter is not string tag) return;

        bool second = _second;
        if (second) OnSecond(SecondKey, EventArgs.Empty);

        switch (tag)
        {
            case "EVAL": Evaluate(); return;
            case "DEL": Vm.Backspace(); return;
            case "AC": Vm.ClearAll(); return;
        }

        string insert = second && Secondary.TryGetValue(tag, out var alt) ? alt : tag;
        Vm.Insert(insert);
    }

    private void Evaluate()
    {
        Vm.Evaluate();
        if (Vm.History.Count > 0)
            HistoryList.ScrollTo(Vm.History.Count - 1, position: ScrollToPosition.End, animate: true);
    }

    private void OnHistoryTapped(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is HistoryEntry h)
        {
            if (h.IsError) Vm.RecallExpression(h.Expression);
            else Vm.RecallResult(h.Result);
            HistoryList.SelectedItem = null;
        }
    }
}
