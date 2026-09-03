using Dissolvers88A.Engine;

int pass = 0, fail = 0;

void Check(string expr, double expected, AngleMode mode = AngleMode.Radians, double tol = 1e-9)
{
    var calc = new Calculator { AngleMode = mode };
    var r = calc.Evaluate(expr);
    if (!r.Ok)
    {
        Console.WriteLine($"FAIL  {expr,-28} -> ERROR({r.Error})  expected {expected}");
        fail++;
        return;
    }
    if (Math.Abs(r.Value - expected) <= tol || (double.IsNaN(expected) && double.IsNaN(r.Value)))
    {
        pass++;
    }
    else
    {
        Console.WriteLine($"FAIL  {expr,-28} -> {r.Value} ({r.Display})  expected {expected}");
        fail++;
    }
}

void CheckDisplay(string expr, string expectedDisplay)
{
    var r = new Calculator().Evaluate(expr);
    if (r.Ok && r.Display == expectedDisplay) { pass++; return; }
    Console.WriteLine($"FAIL  {expr,-28} -> \"{r.Display}\"  expected \"{expectedDisplay}\"");
    fail++;
}

void CheckError(string expr)
{
    var r = new Calculator().Evaluate(expr);
    if (!r.Ok) { pass++; return; }
    Console.WriteLine($"FAIL  {expr,-28} -> {r.Display}  expected an error");
    fail++;
}

bool Near(double a, double b, double t = 1e-6) => Math.Abs(a - b) <= t;
void Report(string name, bool ok) { if (ok) pass++; else { Console.WriteLine($"FAIL  {name}"); fail++; } }

// arithmetic + precedence
Check("1+2*3", 7);
Check("(1+2)*3", 9);
Check("2^3^2", 512);            // right assoc
Check("-2^2", -4);             // negation looser than ^
Check("-3!", -6);
Check("2^-3", 0.125);
Check("10/4", 2.5);
Check("7 mod 3", 1);
Check("2+3(4)", 14);
Check("50%", 0.5);
Check("100*15%", 15);

// implicit multiplication
Check("2(3)", 6);
Check("(1+2)(3+4)", 21);
Check("2pi", 2 * Math.PI);
Check("3e", 3 * Math.E);        // e as constant, not exponent
Check("1e3", 1000);            // e as exponent inside a number
Check("2X", 0);               // X defaults to 0, like a TI-84

// variables
Check("5 STO X", 5);
{
    var c = new Calculator();
    c.Evaluate("3 STO X");
    var r = c.Evaluate("2X^2");   // 2*(X^2) = 18
    if (r.Ok && Math.Abs(r.Value - 18) < 1e-9) pass++; else { Console.WriteLine($"FAIL  2X^2 -> {r.Display}"); fail++; }
}
{
    var c = new Calculator();
    c.Evaluate("6 STO A");
    var r = c.Evaluate("A^2 + 1");
    if (r.Ok && Math.Abs(r.Value - 37) < 1e-9) pass++; else { Console.WriteLine($"FAIL  A^2+1 -> {r.Display}"); fail++; }
}
{
    var c = new Calculator();
    c.Evaluate("3+4");
    var r = c.Evaluate("Ans*2");
    if (r.Ok && Math.Abs(r.Value - 14) < 1e-9) pass++; else { Console.WriteLine($"FAIL  Ans*2 -> {r.Display}"); fail++; }
}

// functions
Check("sqrt(16)", 4);
Check("sqrt(2)^2", 2, tol: 1e-9);
Check("ln(e)", 1);
Check("log(1000)", 3);
Check("log(8,2)", 3);
Check("abs(-5)", 5);
Check("5!", 120);
Check("nCr(5,2)", 10);
Check("nPr(5,2)", 20);
Check("gcd(12,18)", 6);
Check("lcm(4,6)", 12);
Check("min(3,1,4,1,5)", 1);
Check("max(3,1,4,1,5)", 5);
Check("round(3.14159,2)", 3.14);
Check("int(-2.5)", -3);
Check("gamma(5)", 24);

// trig, both modes
Check("sin(0)", 0);
Check("cos(0)", 1);
Check("sin(90)", 1, AngleMode.Degrees, 1e-9);
Check("tan(45)", 1, AngleMode.Degrees, 1e-9);
Check("asin(1)", 90, AngleMode.Degrees, 1e-9);
Check("sin(pi/2)", 1);

// errors
CheckError("sqrt(-1)");
CheckError("1/0");
CheckError("2++");
CheckError("sin()");

// auto-close missing brackets (TI-style)
Check("(1+2", 3);
Check("nCr(10,2", 45);
Check("sin(0", 0);
Check("2(3+4", 14);
Check("sqrt(cos(0", 1);
Check("log(1000", 3);

// formatting
CheckDisplay("1/3", "0.3333333333");
CheckDisplay("2/4", "0.5");
CheckDisplay("1000000000000", "1E12");
CheckDisplay("0.00001", "1E-5");
CheckDisplay("3*4", "12");

// --- distributions ---
Check("normalpdf(0)", 0.39894228, tol: 1e-6);
Check("normalcdf(-100,0)", 0.5, tol: 1e-6);
Check("normalcdf(-1.959964,1.959964)", 0.95, tol: 1e-5);
Check("normalcdf(-1,1)", 0.6826895, tol: 1e-6);
Check("invNorm(0.5)", 0, tol: 1e-6);
Check("invNorm(0.975)", 1.959964, tol: 1e-4);
Check("invNorm(0.95,100,15)", 124.6728, tol: 1e-3);
Check("erf(1)", 0.8427008, tol: 1e-6);
Check("binompdf(10,0.5,5)", 0.24609375, tol: 1e-8);
Check("binomcdf(10,0.5,5)", 0.62304688, tol: 1e-7);
Check("binomcdf(10,0.5,10)", 1.0, tol: 1e-9);
Check("poissonpdf(2,3)", 0.18044705, tol: 1e-7);
Check("poissoncdf(2,3)", 0.85712346, tol: 1e-7);
Check("geometpdf(0.3,3)", 0.147, tol: 1e-9);
Check("geometcdf(0.3,3)", 0.657, tol: 1e-9);
Check("tcdf(-100,0,10)", 0.5, tol: 1e-6);
Check("tcdf(-2,2,10)", 0.92661225, tol: 1e-6);
Check("chi2cdf(0,3.8414588,1)", 0.95, tol: 1e-5);
Check("Fcdf(0,1,10,10)", 0.5, tol: 1e-6);

// --- Stats (direct API) ---
{
    var ov = Dissolvers88A.Engine.Stats.OneVar(new double[] { 1, 2, 3, 4, 5 });
    bool ok = Near(ov.Mean, 3) && Near(ov.Sum, 15) && Near(ov.SampleStdDev, Math.Sqrt(2.5))
              && Near(ov.PopStdDev, Math.Sqrt(2)) && Near(ov.Median, 3) && Near(ov.Q1, 1.5)
              && Near(ov.Q3, 4.5) && ov.N == 5 && Near(ov.Min, 1) && Near(ov.Max, 5);
    Report("OneVar([1..5])", ok);
}
{
    var lr = Dissolvers88A.Engine.Stats.LinReg(new double[] { 1, 2, 3, 4 }, new double[] { 2, 4, 6, 8 });
    Report("LinReg y=2x", Near(lr.A, 2) && Near(lr.B, 0) && Near(lr.R, 1) && Near(lr.R2, 1));
}
{
    var lr = Dissolvers88A.Engine.Stats.LinReg(new double[] { 0, 1, 2, 3, 4 }, new double[] { 1, 3, 2, 5, 4 });
    Report("LinReg slope", Near(lr.A, 0.8, 1e-9) && Near(lr.B, 1.4, 1e-9));
}

Console.WriteLine($"\n{pass} passed, {fail} failed");
return fail == 0 ? 0 : 1;
