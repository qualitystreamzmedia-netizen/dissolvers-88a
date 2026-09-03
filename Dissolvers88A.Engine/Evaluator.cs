namespace Dissolvers88A.Engine;

/// <summary>Walks an <see cref="Node"/> tree and produces a number.</summary>
public static class Evaluator
{
    public static double Eval(Node node, EvalContext ctx) => node switch
    {
        NumberNode n => n.Value,
        VariableNode v => ctx.Resolve(v.Name),
        UnaryNode u => u.Op == '-' ? -Eval(u.Operand, ctx) : Eval(u.Operand, ctx),
        FactorialNode f => Functions.Factorial(Eval(f.Operand, ctx)),
        BinaryNode b => Binary(b.Op, Eval(b.Left, ctx), Eval(b.Right, ctx)),
        CallNode c => Functions.Invoke(c.Name, c.Args.Select(a => Eval(a, ctx)).ToArray(), ctx),
        AssignNode a => Assign(a, ctx),
        _ => throw new CalcException("SYNTAX ERROR")
    };

    private static double Assign(AssignNode a, EvalContext ctx)
    {
        double value = Eval(a.Value, ctx);
        ctx.Store(a.Target, value);
        return value;
    }

    private static double Binary(char op, double l, double r) => op switch
    {
        '+' => l + r,
        '-' => l - r,
        '*' => l * r,
        '/' => l / r, // ±Infinity on divide-by-zero; Calculator turns that into an error
        '^' => Power(l, r),
        _ => throw new CalcException("SYNTAX ERROR")
    };

    private static double Power(double b, double e)
    {
        // (negative base) ^ (odd integer) should stay real
        if (b < 0 && Math.Abs(e - Math.Round(e)) < 1e-12)
        {
            double p = Math.Pow(-b, e);
            return ((long)Math.Round(e) & 1) == 1 ? -p : p;
        }
        return Math.Pow(b, e);
    }
}
