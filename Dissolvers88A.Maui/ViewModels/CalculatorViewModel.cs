using System.Collections.ObjectModel;
using Dissolvers88A.Engine;
using Dissolvers88A.Maui.Mvvm;

namespace Dissolvers88A.Maui.ViewModels;

public sealed class HistoryEntry
{
    public required string Expression { get; init; }
    public required string Result { get; init; }
    public bool IsError { get; init; }
    public Color ResultColor => IsError ? Color.FromArgb("#FCA5A5") : Color.FromArgb("#F8FAFF");
}

/// <summary>Home-screen calculator: history + Ans / variable state (shares the WPF engine).</summary>
public sealed class CalculatorViewModel : ObservableObject
{
    public Calculator Engine { get; } = new();

    public ObservableCollection<HistoryEntry> History { get; } = new();

    private string _input = "";
    /// <summary>The expression being entered (shown on the display line).</summary>
    public string Input
    {
        get => _input;
        private set { if (Set(ref _input, value)) { Raise(nameof(HasInput)); UpdatePreview(_input); } }
    }

    public bool HasInput => _input.Length > 0;

    public void Insert(string text) => Input += text;

    public void Backspace()
    {
        if (_input.Length > 0) Input = _input[..^1];
    }

    public void ClearEntry()
    {
        Input = "";
        Status = "Ready";
    }

    public void RecallExpression(string expr) => Input = expr ?? "";

    public void RecallResult(string result)
    {
        if (!string.IsNullOrEmpty(result) && result != "0") Input += result;
    }

    private string _preview = "";
    public string Preview { get => _preview; private set => Set(ref _preview, value); }

    private string _status = "Ready";
    public string Status { get => _status; private set => Set(ref _status, value); }

    public bool IsDegrees
    {
        get => Engine.AngleMode == AngleMode.Degrees;
        set
        {
            Engine.AngleMode = value ? AngleMode.Degrees : AngleMode.Radians;
            Raise();
            Raise(nameof(AngleLabel));
        }
    }

    public string AngleLabel => IsDegrees ? "DEG" : "RAD";

    public string AnsDisplay => "Ans " + Calculator.Format(Engine.Context.Ans);

    public void ToggleAngle() => IsDegrees = !IsDegrees;

    public void UpdatePreview(string expr)
    {
        expr = (expr ?? "").Trim();
        if (expr.Length == 0) { Preview = ""; return; }
        var r = Engine.Preview(expr);
        Preview = r.Ok ? "= " + r.Display : "";
    }

    /// <summary>Evaluate the current <see cref="Input"/>, push it to history, clear on success.</summary>
    public CalcResult Evaluate()
    {
        string expr = _input.Trim();
        if (expr.Length == 0) return CalcResult.Failure("");

        var r = Engine.Evaluate(expr);
        History.Add(new HistoryEntry
        {
            Expression = expr,
            Result = r.Ok ? r.Display : (r.Error ?? "ERROR"),
            IsError = !r.Ok
        });
        Status = r.Ok ? "Ans = " + r.Display : (r.Error ?? "ERROR");
        Raise(nameof(AnsDisplay));

        if (r.Ok) Input = "";
        else Preview = "";
        return r;
    }

    public void ClearAll()
    {
        History.Clear();
        Engine.Context.Ans = 0;
        Engine.Context.ClearVariables();
        Input = "";
        Preview = "";
        Status = "Cleared";
        Raise(nameof(AnsDisplay));
    }
}
