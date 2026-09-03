namespace Dissolvers88A.Engine;

public sealed record OneVarResult(
    double Mean, double Sum, double SumSq,
    double SampleStdDev, double PopStdDev,
    int N, double Min, double Q1, double Median, double Q3, double Max);

public sealed record TwoVarResult(
    int N,
    double MeanX, double SumX, double SumXSq, double SampleStdDevX, double PopStdDevX,
    double MeanY, double SumY, double SumYSq, double SampleStdDevY, double PopStdDevY,
    double SumXY);

/// <summary>y = a·x + b, with correlation r and r².</summary>
public sealed record LinRegResult(double A, double B, double R, double R2);

/// <summary>1-Var / 2-Var summary statistics and linear regression (the STAT▸CALC core).</summary>
public static class Stats
{
    public static OneVarResult OneVar(IReadOnlyList<double> data)
    {
        int n = data.Count;
        if (n == 0) throw new CalcException("STAT: the list is empty.");

        double sum = 0, sumSq = 0;
        foreach (var v in data) { sum += v; sumSq += v * v; }
        double mean = sum / n;
        double ssd = n > 1 ? Math.Sqrt((sumSq - sum * sum / n) / (n - 1)) : 0;
        double psd = Math.Sqrt((sumSq - sum * sum / n) / n);

        var sorted = data.OrderBy(v => v).ToArray();
        return new OneVarResult(
            mean, sum, sumSq, ssd, psd, n,
            sorted[0], Quartile(sorted, 1), Quartile(sorted, 2), Quartile(sorted, 3), sorted[^1]);
    }

    public static TwoVarResult TwoVar(IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        int n = Math.Min(xs.Count, ys.Count);
        if (n == 0) throw new CalcException("STAT: need paired X and Y values.");

        double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0;
        for (int i = 0; i < n; i++)
        {
            sx += xs[i]; sy += ys[i];
            sxx += xs[i] * xs[i]; syy += ys[i] * ys[i];
            sxy += xs[i] * ys[i];
        }
        double mx = sx / n, my = sy / n;
        double ssdx = n > 1 ? Math.Sqrt((sxx - sx * sx / n) / (n - 1)) : 0;
        double psdx = Math.Sqrt((sxx - sx * sx / n) / n);
        double ssdy = n > 1 ? Math.Sqrt((syy - sy * sy / n) / (n - 1)) : 0;
        double psdy = Math.Sqrt((syy - sy * sy / n) / n);

        return new TwoVarResult(n, mx, sx, sxx, ssdx, psdx, my, sy, syy, ssdy, psdy, sxy);
    }

    /// <summary>Least-squares fit y = a·x + b.</summary>
    public static LinRegResult LinReg(IReadOnlyList<double> xs, IReadOnlyList<double> ys)
    {
        int n = Math.Min(xs.Count, ys.Count);
        if (n < 2) throw new CalcException("LinReg: need at least two points.");

        double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0;
        for (int i = 0; i < n; i++)
        {
            sx += xs[i]; sy += ys[i];
            sxx += xs[i] * xs[i]; syy += ys[i] * ys[i];
            sxy += xs[i] * ys[i];
        }
        double denom = n * sxx - sx * sx;
        if (denom == 0) throw new CalcException("LinReg: X values are all the same.");

        double a = (n * sxy - sx * sy) / denom;
        double b = (sy - a * sx) / n;

        double rDen = Math.Sqrt((n * sxx - sx * sx) * (n * syy - sy * sy));
        double r = rDen == 0 ? 0 : (n * sxy - sx * sy) / rDen;

        return new LinRegResult(a, b, r, r * r);
    }

    /// <summary>TI-84 quartile: median of the lower / upper half (excluding the overall median when n is odd).</summary>
    private static double Quartile(double[] sorted, int q)
    {
        int n = sorted.Length;
        if (q == 2) return Median(sorted, 0, n);
        int half = n / 2;
        return q == 1
            ? Median(sorted, 0, half)
            : Median(sorted, n - half, n);
    }

    private static double Median(double[] s, int from, int toExclusive)
    {
        int len = toExclusive - from;
        if (len == 0) return double.NaN;
        int mid = from + len / 2;
        return len % 2 == 1 ? s[mid] : (s[mid - 1] + s[mid]) / 2.0;
    }
}
