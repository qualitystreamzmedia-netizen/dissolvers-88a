using System.Collections.ObjectModel;
using Dissolvers88A.Engine;
using Dissolvers88A.Mvvm;

namespace Dissolvers88A.ViewModels;

public sealed class HistoryEntry
{
    public required string Expression { get; init; }
    public required string Result { get; init; }
    public bool IsError { get; init; }
}

/// <summary>Home-screen calculator: a running history plus Ans / variable state.</summary>
public sealed class CalculatorViewModel : ObservableObject
{
    public Calculator Engine { get; } = new();

    public ObservableCollection<HistoryEntry> History { get; } = new();

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
            Raise(nameof(IsRadians));
        }
    }

    public bool IsRadians => Engine.AngleMode == AngleMode.Radians;

    public string AnsDisplay => Calculator.Format(Engine.Context.Ans);

    /// <summary>Live "= …" shown under the input as the user types.</summary>
    public void UpdatePreview(string expr)
    {
        expr = expr.Trim();
        if (expr.Length == 0) { Preview = ""; return; }
        var r = Engine.Preview(expr);
        Preview = r.Ok ? "= " + r.Display : "";
    }

    /// <summary>Evaluate a line, push it to history, return the result.</summary>
    public CalcResult Commit(string expr)
    {
        expr = expr.Trim();
        if (expr.Length == 0) return CalcResult.Failure("");

        var r = Engine.Evaluate(expr);
        History.Add(new HistoryEntry
        {
            Expression = expr,
            Result = r.Ok ? r.Display : (r.Error ?? "ERROR"),
            IsError = !r.Ok
        });
        Status = r.Ok ? "Ans = " + r.Display : (r.Error ?? "ERROR");
        Preview = "";
        Raise(nameof(AnsDisplay));
        return r;
    }

    public void ClearAll()
    {
        History.Clear();
        Engine.Context.Ans = 0;
        Engine.Context.ClearVariables();
        Preview = "";
        Status = "Cleared";
        Raise(nameof(AnsDisplay));
    }
}
