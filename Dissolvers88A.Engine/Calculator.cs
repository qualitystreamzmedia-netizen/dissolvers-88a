using System.Globalization;

namespace Dissolvers88A.Engine;

public readonly record struct CalcResult(bool Ok, double Value, string Display, string? Error)
{
    public static CalcResult Success(double v, string display) => new(true, v, display, null);
    public static CalcResult Failure(string error) => new(false, double.NaN, error, error);
}

/// <summary>
/// The public entry point for the calculator screen. Holds the running
/// <see cref="EvalContext"/> (angle mode, variables, Ans) and turns a line of
/// input into a formatted result the way a TI-84 home screen would.
/// </summary>
public sealed class Calculator
{
    public EvalContext Context { get; } = new();

    public AngleMode AngleMode
    {
        get => Context.AngleMode;
        set => Context.AngleMode = value;
    }

    /// <summary>Number of significant digits shown (TI-84 shows 10).</summary>
    public int DisplayDigits { get; set; } = 10;

    /// <summary>Evaluate without committing to Ans or storing variables — for a live preview.</summary>
    public CalcResult Preview(string input)
    {
        var snap = Context.Snapshot();
        try { return Evaluate(input, commit: false); }
        finally { Context.Restore(snap); }
    }

    public CalcResult Evaluate(string input) => Evaluate(input, commit: true);

    private CalcResult Evaluate(string input, bool commit)
    {
        if (string.IsNullOrWhiteSpace(input))
            return CalcResult.Failure("");

        input = NormalizeStoreArrow(input);

        Node ast;
        try
        {
            ast = new Parser(Lexer.Tokenize(input)).ParseProgram();
        }
        catch (CalcException ex)
        {
            return CalcResult.Failure(ex.Message);
        }
        catch (Exception)
        {
            return CalcResult.Failure("SYNTAX ERROR");
        }

        double value;
        try
        {
            value = Evaluator.Eval(ast, Context);
        }
        catch (CalcException ex)
        {
            return CalcResult.Failure(ex.Message);
        }
        catch (DivideByZeroException)
        {
            return CalcResult.Failure("DIVIDE BY 0");
        }
        catch (Exception)
        {
            return CalcResult.Failure("ERROR");
        }

        if (double.IsNaN(value)) return CalcResult.Failure("NONREAL ANSWER");
        if (double.IsPositiveInfinity(value)) return CalcResult.Failure("OVERFLOW");
        if (double.IsNegativeInfinity(value)) return CalcResult.Failure("OVERFLOW");

        if (commit) Context.Ans = value;
        return CalcResult.Success(value, Format(value, DisplayDigits));
    }

    /// <summary>Evaluate a bare expression against a supplied X (used by the grapher / table).</summary>
    public double EvaluateAt(Node compiled, double x)
    {
        Context.Store("X", x);
        return Evaluator.Eval(compiled, Context);
    }

    public static Node Compile(string expression) =>
        new Parser(Lexer.Tokenize(expression)).ParseProgram();

    private static string NormalizeStoreArrow(string s) =>
        s.Replace("→", " STO ").Replace("->", " STO ").Replace("➔", " STO ");

    // ---- number formatting ------------------------------------------------

    public static string Format(double v, int digits = 10)
    {
        if (v == 0) return "0";

        // clean up floating-point fuzz near an integer
        double rounded = Math.Round(v);
        if (rounded != 0 && Math.Abs(v - rounded) <= 1e-10 * Math.Abs(rounded))
            v = rounded;

        double abs = Math.Abs(v);

        // TI switches to scientific outside roughly 1e-4 .. 1e10
        if (abs != 0 && (abs >= 1e10 || abs < 1e-4))
            return Scientific(v, digits);

        string s = v.ToString("G" + digits, CultureInfo.InvariantCulture);
        if (s.Contains('E'))
            return Scientific(v, digits);
        return s;
    }

    private static string Scientific(double v, int digits)
    {
        string s = v.ToString("E" + (digits - 1), CultureInfo.InvariantCulture); // e.g. 1.234560000E-005
        int e = s.IndexOf('E');
        string mantissa = s[..e].TrimEnd('0').TrimEnd('.');
        int exp = int.Parse(s[(e + 1)..], CultureInfo.InvariantCulture);
        return $"{mantissa}E{exp}";
    }
}
