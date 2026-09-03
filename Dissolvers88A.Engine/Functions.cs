namespace Dissolvers88A.Engine;

/// <summary>
/// The built-in function table. Names are matched case-insensitively and the
/// common TI spellings are aliased (arcsin ≡ asin ≡ sin⁻¹, sqroot ≡ sqrt, …).
/// Trig respects <see cref="EvalContext.AngleMode"/>.
/// </summary>
public static class Functions
{
    private delegate double Impl(double[] a, EvalContext ctx);

    private readonly record struct Spec(int Min, int Max, Impl Fn);

    private static readonly Dictionary<string, Spec> Table = Build();

    public static bool IsFunction(string name) => Table.ContainsKey(Key(name));

    public static double Invoke(string name, double[] args, EvalContext ctx)
    {
        if (!Table.TryGetValue(Key(name), out var spec))
            throw new CalcException($"Unknown function '{name}'.");
        if (args.Length < spec.Min || args.Length > spec.Max)
            throw new CalcException($"{name}: wrong number of arguments.");
        return spec.Fn(args, ctx);
    }

    /// <summary>Every name the parser should treat as a function (for name completion / syntax hints).</summary>
    public static IEnumerable<string> Names => Table.Keys;

    private static string Key(string name) => name.ToLowerInvariant() switch
    {
        "arcsin" or "sin⁻¹" or "asine" => "asin",
        "arccos" or "cos⁻¹" => "acos",
        "arctan" or "tan⁻¹" => "atan",
        "arcsinh" => "asinh",
        "arccosh" => "acosh",
        "arctanh" => "atanh",
        "sqroot" or "squareroot" or "√" => "sqrt",
        "cuberoot" => "cbrt",
        "naturallog" => "ln",
        "logten" => "log",
        "absolutevalue" or "absval" => "abs",
        "fact" or "factorial" => "gamma", // n! via Γ(n+1) when called as a function
        "ipart" => "intpart",
        "fracpart" => "fpart",
        "remainder" => "rem",
        var s => s
    };

    // ---- angle helpers -------------------------------------------------------
    private static double ToRad(double x, EvalContext c) => c.AngleMode == AngleMode.Degrees ? x * Math.PI / 180.0 : x;
    private static double FromRad(double x, EvalContext c) => c.AngleMode == AngleMode.Degrees ? x * 180.0 / Math.PI : x;

    private static Dictionary<string, Spec> Build()
    {
        var t = new Dictionary<string, Spec>(StringComparer.Ordinal);

        void Add(string name, int min, int max, Impl fn) => t[name.ToLowerInvariant()] = new Spec(min, max, fn);
        void Fn1(string name, Func<double, double> f) => Add(name, 1, 1, (a, _) => f(a[0]));

        // --- trigonometry (angle-mode aware) ---
        Add("sin", 1, 1, (a, c) => Math.Sin(ToRad(a[0], c)));
        Add("cos", 1, 1, (a, c) => Math.Cos(ToRad(a[0], c)));
        Add("tan", 1, 1, (a, c) => Math.Tan(ToRad(a[0], c)));
        Add("csc", 1, 1, (a, c) => 1.0 / Math.Sin(ToRad(a[0], c)));
        Add("sec", 1, 1, (a, c) => 1.0 / Math.Cos(ToRad(a[0], c)));
        Add("cot", 1, 1, (a, c) => 1.0 / Math.Tan(ToRad(a[0], c)));
        Add("asin", 1, 1, (a, c) => FromRad(Math.Asin(a[0]), c));
        Add("acos", 1, 1, (a, c) => FromRad(Math.Acos(a[0]), c));
        Add("atan", 1, 1, (a, c) => FromRad(Math.Atan(a[0]), c));
        Add("atan2", 2, 2, (a, c) => FromRad(Math.Atan2(a[0], a[1]), c));

        // --- hyperbolic ---
        Fn1("sinh", Math.Sinh); Fn1("cosh", Math.Cosh); Fn1("tanh", Math.Tanh);
        Fn1("asinh", Math.Asinh); Fn1("acosh", Math.Acosh); Fn1("atanh", Math.Atanh);

        // --- exp / log ---
        Fn1("ln", Math.Log);
        Fn1("exp", Math.Exp);
        Fn1("sqrt", x => x < 0 ? double.NaN : Math.Sqrt(x));
        Fn1("cbrt", Math.Cbrt);
        Fn1("log2", Math.Log2);
        Add("log", 1, 2, (a, _) => a.Length == 1 ? Math.Log10(a[0]) : Math.Log(a[0]) / Math.Log(a[1]));
        Add("logbase", 2, 2, (a, _) => Math.Log(a[0]) / Math.Log(a[1]));
        Add("root", 2, 2, (a, _) => NthRoot(a[1], a[0]));   // root(index, x)  ->  x^(1/index)
        Add("nthroot", 2, 2, (a, _) => NthRoot(a[0], a[1])); // nthroot(x, index)
        Add("pow", 2, 2, (a, _) => Math.Pow(a[0], a[1]));

        // --- rounding / parts ---
        Fn1("abs", Math.Abs);
        Fn1("sign", x => Math.Sign(x));
        Fn1("sgn", x => Math.Sign(x));
        Fn1("floor", Math.Floor);
        Fn1("ceil", Math.Ceiling);
        Add("int", 1, 1, (a, _) => Math.Floor(a[0]));       // TI int() is floor
        Add("intpart", 1, 1, (a, _) => Math.Truncate(a[0]));
        Add("fpart", 1, 1, (a, _) => a[0] - Math.Truncate(a[0]));
        Add("round", 1, 2, (a, _) => a.Length == 1
            ? Math.Round(a[0], MidpointRounding.AwayFromZero)
            : Math.Round(a[0], (int)Math.Clamp(a[1], 0, 15), MidpointRounding.AwayFromZero));

        // --- combinatorics / number theory ---
        Add("gamma", 1, 1, (a, _) => Gamma(a[0]));
        Add("nPr", 2, 2, (a, _) => Permutations(a[0], a[1]));
        Add("nCr", 2, 2, (a, _) => Combinations(a[0], a[1]));
        Add("gcd", 2, 2, (a, _) => Gcd((long)Math.Round(Math.Abs(a[0])), (long)Math.Round(Math.Abs(a[1]))));
        Add("lcm", 2, 2, (a, _) =>
        {
            long x = (long)Math.Round(Math.Abs(a[0])), y = (long)Math.Round(Math.Abs(a[1]));
            long g = Gcd(x, y);
            return g == 0 ? 0 : x / g * y;
        });
        Add("mod", 2, 2, (a, _) => a[0] - a[1] * Math.Floor(a[0] / a[1]));       // result follows divisor sign
        Add("rem", 2, 2, (a, _) => Math.IEEERemainder(a[0], a[1]) is var r && Math.Sign(r) != 0 && Math.Sign(r) != Math.Sign(a[0])
            ? r + a[1] * Math.Sign(a[0]) * Math.Sign(a[1]) : a[0] % a[1]);

        // --- lists-as-varargs ---
        Add("min", 1, 64, (a, _) => a.Min());
        Add("max", 1, 64, (a, _) => a.Max());
        Add("mean", 1, 64, (a, _) => a.Average());
        Add("sum", 1, 64, (a, _) => a.Sum());
        Add("product", 1, 64, (a, _) => a.Aggregate(1.0, (p, x) => p * x));
        Add("stdev", 2, 64, (a, _) => SampleStdDev(a));
        Add("variance", 2, 64, (a, _) => { double s = SampleStdDev(a); return s * s; });

        // --- conversions / misc ---
        Add("radians", 1, 1, (a, _) => a[0] * Math.PI / 180.0);
        Add("degrees", 1, 1, (a, _) => a[0] * 180.0 / Math.PI);
        Add("dms", 1, 1, (a, _) => { double d = Math.Truncate(a[0]); double m = (a[0] - d) * 60; double mm = Math.Truncate(m); double ss = (m - mm) * 60; return d + mm / 100 + ss / 10000; });

        // --- probability ---
        Add("rand", 0, 0, (_, c) => c.NextRandom());
        Add("randInt", 2, 3, (a, c) => c.RandomInt(a[0], a[1]));

        // --- distributions (2nd DISTR) ---
        Add("normalpdf", 1, 3, (a, _) => Distributions.NormalPdf(a[0], G(a, 1, 0), G(a, 2, 1)));
        Add("normalcdf", 2, 4, (a, _) => Distributions.NormalCdf(a[0], a[1], G(a, 2, 0), G(a, 3, 1)));
        Add("invNorm",   1, 3, (a, _) => Distributions.InvNorm(a[0], G(a, 1, 0), G(a, 2, 1)));
        Add("erf",  1, 1, (a, _) => SpecialFunctions.Erf(a[0]));
        Add("erfc", 1, 1, (a, _) => SpecialFunctions.Erfc(a[0]));

        Add("binompdf", 3, 3, (a, _) => Distributions.BinomPdf(a[0], a[1], a[2]));
        Add("binomcdf", 3, 3, (a, _) => Distributions.BinomCdf(a[0], a[1], a[2]));
        Add("poissonpdf", 2, 2, (a, _) => Distributions.PoissonPdf(a[0], a[1]));
        Add("poissoncdf", 2, 2, (a, _) => Distributions.PoissonCdf(a[0], a[1]));
        Add("geometpdf", 2, 2, (a, _) => Distributions.GeometPdf(a[0], a[1]));
        Add("geometcdf", 2, 2, (a, _) => Distributions.GeometCdf(a[0], a[1]));

        Add("tpdf", 2, 2, (a, _) => Distributions.TPdf(a[0], a[1]));
        Add("tcdf", 3, 3, (a, _) => Distributions.TCdf(a[0], a[1], a[2]));
        Add("chi2pdf", 2, 2, (a, _) => Distributions.Chi2Pdf(a[0], a[1]));
        Add("chi2cdf", 3, 3, (a, _) => Distributions.Chi2Cdf(a[0], a[1], a[2]));
        Add("Fpdf", 3, 3, (a, _) => Distributions.FPdf(a[0], a[1], a[2]));
        Add("Fcdf", 4, 4, (a, _) => Distributions.FCdf(a[0], a[1], a[2], a[3]));

        return t;
    }

    /// <summary>Optional argument [i] with a default.</summary>
    private static double G(double[] a, int i, double fallback) => i < a.Length ? a[i] : fallback;

    // ---- helpers ------------------------------------------------------------

    private static double NthRoot(double x, double index)
    {
        if (index == 0) return double.NaN;
        if (x < 0)
        {
            long k = (long)Math.Round(index);
            if (Math.Abs(k - index) < 1e-9 && (k & 1) == 1) return -Math.Pow(-x, 1.0 / index);
            return double.NaN;
        }
        return Math.Pow(x, 1.0 / index);
    }

    private static long Gcd(long a, long b) { while (b != 0) (a, b) = (b, a % b); return a; }

    internal static double Factorial(double x)
    {
        if (x < 0 || Math.Abs(x - Math.Round(x)) > 1e-9)
        {
            double g = Gamma(x + 1);
            if (double.IsNaN(g)) throw new CalcException("DOMAIN: factorial needs a non-negative integer.");
            return g;
        }
        int n = (int)Math.Round(x);
        if (n > 170) return double.PositiveInfinity;
        double r = 1;
        for (int k = 2; k <= n; k++) r *= k;
        return r;
    }

    private static double Permutations(double n, double r)
    {
        if (AreIntegers(n, r) && r >= 0 && n >= r) return Factorial(n) / Factorial(n - r);
        return Gamma(n + 1) / Gamma(n - r + 1);
    }

    private static double Combinations(double n, double r)
    {
        if (AreIntegers(n, r) && r >= 0 && n >= r)
        {
            r = Math.Min(r, n - r);
            double result = 1;
            for (int k = 0; k < r; k++) result = result * (n - k) / (k + 1);
            return Math.Round(result);
        }
        return Gamma(n + 1) / (Gamma(r + 1) * Gamma(n - r + 1));
    }

    private static bool AreIntegers(params double[] xs) => xs.All(x => Math.Abs(x - Math.Round(x)) < 1e-9);

    private static double SampleStdDev(double[] a)
    {
        double mean = a.Average();
        double sq = a.Sum(x => (x - mean) * (x - mean));
        return Math.Sqrt(sq / (a.Length - 1));
    }

    /// <summary>Lanczos approximation of the Gamma function (good to ~1e-13).</summary>
    internal static double Gamma(double z)
    {
        double[] g =
        {
            676.5203681218851, -1259.1392167224028, 771.32342877765313,
            -176.61502916214059, 12.507343278686905, -0.13857109526572012,
            9.9843695780195716e-6, 1.5056327351493116e-7
        };
        if (z < 0.5)
            return Math.PI / (Math.Sin(Math.PI * z) * Gamma(1 - z));
        z -= 1;
        double x = 0.99999999999980993;
        for (int i = 0; i < g.Length; i++) x += g[i] / (z + i + 1);
        double tval = z + g.Length - 0.5;
        return Math.Sqrt(2 * Math.PI) * Math.Pow(tval, z + 0.5) * Math.Exp(-tval) * x;
    }
}
