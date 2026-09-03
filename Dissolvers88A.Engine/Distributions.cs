namespace Dissolvers88A.Engine;

/// <summary>
/// The probability distributions from the TI-84's 2nd▸DISTR menu. All are
/// exposed to the calculator through <see cref="Functions"/> (normalcdf,
/// invNorm, binompdf, tcdf, …).
/// </summary>
public static class Distributions
{
    private static readonly double Sqrt2Pi = Math.Sqrt(2 * Math.PI);
    private static readonly double Sqrt2 = Math.Sqrt(2);

    // ---- normal --------------------------------------------------------

    public static double NormalPdf(double x, double mu = 0, double sigma = 1)
        => Math.Exp(-0.5 * Sq((x - mu) / sigma)) / (sigma * Sqrt2Pi);

    public static double StdNormalCdf(double z) => 0.5 * (1 + SpecialFunctions.Erf(z / Sqrt2));

    /// <summary>P(lo ≤ X ≤ hi) for X ~ N(mu, sigma²).</summary>
    public static double NormalCdf(double lo, double hi, double mu = 0, double sigma = 1)
        => StdNormalCdf((hi - mu) / sigma) - StdNormalCdf((lo - mu) / sigma);

    /// <summary>The x with P(X ≤ x) = area, X ~ N(mu, sigma²). Acklam's rational approximation.</summary>
    public static double InvNorm(double area, double mu = 0, double sigma = 1)
    {
        if (area <= 0) return double.NegativeInfinity;
        if (area >= 1) return double.PositiveInfinity;

        double[] a = { -3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02, 1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00 };
        double[] b = { -5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02, 6.680131188771972e+01, -1.328068155288572e+01 };
        double[] c = { -7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00, -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00 };
        double[] d = { 7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00, 3.754408661907416e+00 };
        const double pLow = 0.02425, pHigh = 1 - 0.02425;
        double q, r, z;

        if (area < pLow)
        {
            q = Math.Sqrt(-2 * Math.Log(area));
            z = (((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }
        else if (area <= pHigh)
        {
            q = area - 0.5; r = q * q;
            z = (((((a[0] * r + a[1]) * r + a[2]) * r + a[3]) * r + a[4]) * r + a[5]) * q /
                (((((b[0] * r + b[1]) * r + b[2]) * r + b[3]) * r + b[4]) * r + 1);
        }
        else
        {
            q = Math.Sqrt(-2 * Math.Log(1 - area));
            z = -(((((c[0] * q + c[1]) * q + c[2]) * q + c[3]) * q + c[4]) * q + c[5]) /
                ((((d[0] * q + d[1]) * q + d[2]) * q + d[3]) * q + 1);
        }

        // one Halley refinement step
        double e = StdNormalCdf(z) - area;
        double u = e * Sqrt2Pi * Math.Exp(z * z / 2);
        z -= u / (1 + z * u / 2);

        return mu + sigma * z;
    }

    // ---- binomial -----------------------------------------------------

    public static double BinomPdf(double n, double p, double k)
    {
        if (k < 0 || k > n || p < 0 || p > 1) return 0;
        double lnC = SpecialFunctions.LnGamma(n + 1) - SpecialFunctions.LnGamma(k + 1) - SpecialFunctions.LnGamma(n - k + 1);
        return Math.Exp(lnC + k * SafeLog(p) + (n - k) * SafeLog(1 - p));
    }

    /// <summary>P(X ≤ k) for X ~ Binomial(n, p).</summary>
    public static double BinomCdf(double n, double p, double k)
    {
        double kk = Math.Floor(k);
        if (kk < 0) return 0;
        if (kk >= n) return 1;
        // I_{1-p}(n-k, k+1)
        return SpecialFunctions.BetaI(1 - p, n - kk, kk + 1);
    }

    // ---- Poisson ----------------------------------------------------

    public static double PoissonPdf(double lambda, double k)
    {
        if (k < 0 || Math.Abs(k - Math.Round(k)) > 1e-9) return 0;
        return Math.Exp(-lambda + k * Math.Log(lambda) - SpecialFunctions.LnGamma(k + 1));
    }

    public static double PoissonCdf(double lambda, double k)
    {
        double kk = Math.Floor(k);
        return kk < 0 ? 0 : SpecialFunctions.GammaQ(kk + 1, lambda);
    }

    // ---- geometric (trials until first success, k = 1, 2, …) --------

    public static double GeometPdf(double p, double k)
        => k < 1 || Math.Abs(k - Math.Round(k)) > 1e-9 ? 0 : p * Math.Pow(1 - p, k - 1);

    public static double GeometCdf(double p, double k)
        => k < 1 ? 0 : 1 - Math.Pow(1 - p, Math.Floor(k));

    // ---- Student's t ---------------------------------------------

    public static double TPdf(double x, double df)
    {
        double lnNorm = SpecialFunctions.LnGamma((df + 1) / 2) - SpecialFunctions.LnGamma(df / 2)
                        - 0.5 * Math.Log(df * Math.PI);
        return Math.Exp(lnNorm - (df + 1) / 2 * Math.Log(1 + x * x / df));
    }

    public static double TCdf(double lo, double hi, double df) => TCdfLower(hi, df) - TCdfLower(lo, df);

    private static double TCdfLower(double t, double df)
    {
        if (double.IsNegativeInfinity(t)) return 0;
        if (double.IsPositiveInfinity(t)) return 1;
        double xb = df / (df + t * t);
        double p = 0.5 * SpecialFunctions.BetaI(xb, df / 2, 0.5);
        return t >= 0 ? 1 - p : p;
    }

    // ---- chi-square ---------------------------------------------

    public static double Chi2Pdf(double x, double df)
    {
        if (x <= 0) return 0;
        return Math.Exp((df / 2 - 1) * Math.Log(x) - x / 2 - df / 2 * Math.Log(2) - SpecialFunctions.LnGamma(df / 2));
    }

    public static double Chi2Cdf(double lo, double hi, double df) => Chi2CdfLower(hi, df) - Chi2CdfLower(lo, df);

    private static double Chi2CdfLower(double x, double df)
        => x <= 0 ? 0 : double.IsPositiveInfinity(x) ? 1 : SpecialFunctions.GammaP(df / 2, x / 2);

    // ---- F ----------------------------------------------------

    public static double FPdf(double x, double d1, double d2)
    {
        if (x <= 0) return 0;
        double lnB = SpecialFunctions.LnGamma((d1 + d2) / 2) - SpecialFunctions.LnGamma(d1 / 2) - SpecialFunctions.LnGamma(d2 / 2);
        return Math.Exp(lnB + d1 / 2 * Math.Log(d1) + d2 / 2 * Math.Log(d2)
                        + (d1 / 2 - 1) * Math.Log(x) - (d1 + d2) / 2 * Math.Log(d2 + d1 * x));
    }

    public static double FCdf(double lo, double hi, double d1, double d2) => FCdfLower(hi, d1, d2) - FCdfLower(lo, d1, d2);

    private static double FCdfLower(double x, double d1, double d2)
        => x <= 0 ? 0 : double.IsPositiveInfinity(x) ? 1 : SpecialFunctions.BetaI(d1 * x / (d1 * x + d2), d1 / 2, d2 / 2);

    // ---- helpers --------------------------------------------------

    private static double Sq(double v) => v * v;
    private static double SafeLog(double v) => v <= 0 ? double.NegativeInfinity : Math.Log(v);
}
