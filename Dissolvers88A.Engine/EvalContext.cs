namespace Dissolvers88A.Engine;

public enum AngleMode { Radians, Degrees }

/// <summary>A calculator error surfaced to the user (SYNTAX ERROR, DOMAIN, etc.).</summary>
public sealed class CalcException : Exception
{
    public CalcException(string message) : base(message) { }
}

/// <summary>
/// Mutable state the evaluator reads: the angle mode, the 27 letter variables
/// (A–Z plus θ), the graphing variable X, and the last answer (Ans).
/// </summary>
public sealed class EvalContext
{
    public AngleMode AngleMode { get; set; } = AngleMode.Radians;

    public double Ans { get; set; }

    private readonly Dictionary<string, double> _vars = new(StringComparer.Ordinal);

    private readonly Random _random = new();

    public double NextRandom() => _random.NextDouble();

    public double RandomInt(double lo, double hi)
    {
        long a = (long)Math.Round(lo), b = (long)Math.Round(hi);
        if (b < a) (a, b) = (b, a);
        return _random.NextInt64(a, b + 1);
    }

    /// <summary>Reads a variable or a named constant. Unknown letters read as 0, like a TI-84.</summary>
    public double Resolve(string name)
    {
        switch (name.ToLowerInvariant())
        {
            case "pi": case "π": return Math.PI;
            case "e": return Math.E;
            case "ans": return Ans;
            case "true": return 1;
            case "false": return 0;
        }
        return _vars.TryGetValue(Canonical(name), out var v) ? v : 0.0;
    }

    public void Store(string name, double value)
    {
        var key = Canonical(name);
        if (key is "pi" or "e" or "ans")
            throw new CalcException("Cannot store into a constant.");
        _vars[key] = value;
    }

    public IReadOnlyDictionary<string, double> Variables => _vars;

    public void ClearVariables() => _vars.Clear();

    /// <summary>Capture the mutable state so a preview evaluation can be rolled back.</summary>
    public (double ans, Dictionary<string, double> vars) Snapshot() => (Ans, new(_vars));

    public void Restore((double ans, Dictionary<string, double> vars) snap)
    {
        Ans = snap.ans;
        _vars.Clear();
        foreach (var kv in snap.vars) _vars[kv.Key] = kv.Value;
    }

    /// <summary>Single letters fold to upper-case; multi-letter names stay as typed.</summary>
    private static string Canonical(string name) =>
        name.Length == 1 && char.IsLetter(name[0]) ? name.ToUpperInvariant() : name;
}
