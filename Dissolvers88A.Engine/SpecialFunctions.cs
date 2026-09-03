namespace Dissolvers88A.Engine;

/// <summary>
/// Numerical special functions used by the statistics distributions:
/// log-gamma, the regularized incomplete gamma P(a,x), the regularized
/// incomplete beta I_x(a,b), and the error function. Standard
/// series / continued-fraction implementations (Numerical Recipes style),
/// accurate to ~1e-12 across the ranges a calculator needs.
/// </summary>
public static class SpecialFunctions
{
    private const int MaxIter = 300;
    private const double Epsilon = 1e-14;
    private const double Tiny = 1e-300;

    private static readonly double[] LanczosG =
    {
        676.5203681218851, -1259.1392167224028, 771.32342877765313,
        -176.61502916214059, 12.507343278686905, -0.13857109526572012,
        9.9843695780195716e-6, 1.5056327351493116e-7
    };

    public static double Gamma(double z)
    {
        if (z < 0.5) return Math.PI / (Math.Sin(Math.PI * z) * Gamma(1 - z));
        z -= 1;
        double x = 0.99999999999980993;
        for (int i = 0; i < LanczosG.Length; i++) x += LanczosG[i] / (z + i + 1);
        double t = z + LanczosG.Length - 0.5;
        return Math.Sqrt(2 * Math.PI) * Math.Pow(t, z + 0.5) * Math.Exp(-t) * x;
    }

    public static double LnGamma(double z)
    {
        if (z < 0.5) return Math.Log(Math.PI / Math.Sin(Math.PI * z)) - LnGamma(1 - z);
        z -= 1;
        double x = 0.99999999999980993;
        for (int i = 0; i < LanczosG.Length; i++) x += LanczosG[i] / (z + i + 1);
        double t = z + LanczosG.Length - 0.5;
        return 0.5 * Math.Log(2 * Math.PI) + (z + 0.5) * Math.Log(t) - t + Math.Log(x);
    }

    /// <summary>Regularized lower incomplete gamma P(a,x) = γ(a,x)/Γ(a).</summary>
    public static double GammaP(double a, double x)
    {
        if (x < 0 || a <= 0) return double.NaN;
        if (x == 0) return 0;
        return x < a + 1 ? GammaSeries(a, x) : 1.0 - GammaContinuedFraction(a, x);
    }

    public static double GammaQ(double a, double x) => 1.0 - GammaP(a, x);

    private static double GammaSeries(double a, double x)
    {
        double ap = a, sum = 1.0 / a, del = sum;
        for (int n = 0; n < MaxIter; n++)
        {
            ap += 1;
            del *= x / ap;
            sum += del;
            if (Math.Abs(del) < Math.Abs(sum) * Epsilon) break;
        }
        return sum * Math.Exp(-x + a * Math.Log(x) - LnGamma(a));
    }

    private static double GammaContinuedFraction(double a, double x)
    {
        double b = x + 1 - a;
        double c = 1 / Tiny;
        double d = 1 / b;
        double h = d;
        for (int i = 1; i < MaxIter; i++)
        {
            double an = -i * (i - a);
            b += 2;
            d = an * d + b; if (Math.Abs(d) < Tiny) d = Tiny;
            c = b + an / c; if (Math.Abs(c) < Tiny) c = Tiny;
            d = 1 / d;
            double del = d * c;
            h *= del;
            if (Math.Abs(del - 1) < Epsilon) break;
        }
        return Math.Exp(-x + a * Math.Log(x) - LnGamma(a)) * h;
    }

    /// <summary>Regularized incomplete beta I_x(a,b).</summary>
    public static double BetaI(double x, double a, double b)
    {
        if (a <= 0 || b <= 0) return double.NaN;
        if (x <= 0) return 0;
        if (x >= 1) return 1;
        double bt = Math.Exp(LnGamma(a + b) - LnGamma(a) - LnGamma(b)
                             + a * Math.Log(x) + b * Math.Log(1 - x));
        return x < (a + 1) / (a + b + 2)
            ? bt * BetaContinuedFraction(x, a, b) / a
            : 1.0 - bt * BetaContinuedFraction(1 - x, b, a) / b;
    }

    private static double BetaContinuedFraction(double x, double a, double b)
    {
        double qab = a + b, qap = a + 1, qam = a - 1;
        double c = 1;
        double d = 1 - qab * x / qap; if (Math.Abs(d) < Tiny) d = Tiny;
        d = 1 / d;
        double h = d;
        for (int m = 1; m < MaxIter; m++)
        {
            int m2 = 2 * m;
            double aa = m * (b - m) * x / ((qam + m2) * (a + m2));
            d = 1 + aa * d; if (Math.Abs(d) < Tiny) d = Tiny;
            c = 1 + aa / c; if (Math.Abs(c) < Tiny) c = Tiny;
            d = 1 / d;
            h *= d * c;
            aa = -(a + m) * (qab + m) * x / ((a + m2) * (qap + m2));
            d = 1 + aa * d; if (Math.Abs(d) < Tiny) d = Tiny;
            c = 1 + aa / c; if (Math.Abs(c) < Tiny) c = Tiny;
            d = 1 / d;
            double del = d * c;
            h *= del;
            if (Math.Abs(del - 1) < Epsilon) break;
        }
        return h;
    }

    /// <summary>Error function erf(x), via the incomplete gamma.</summary>
    public static double Erf(double x) =>
        x < 0 ? -GammaP(0.5, x * x) : GammaP(0.5, x * x);

    public static double Erfc(double x) => 1.0 - Erf(x);
}
