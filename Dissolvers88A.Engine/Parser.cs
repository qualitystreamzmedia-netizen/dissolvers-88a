namespace Dissolvers88A.Engine;

/// <summary>
/// Recursive-descent parser with the same precedence a TI-84 uses:
/// negation binds looser than <c>^</c> ( -2^2 = -4 ), <c>^</c> is
/// right-associative, factorial is postfix, and juxtaposition means
/// multiplication ( 2x, 2(3), 2π, (1+2)(3+4), 3sin(x) ).
/// </summary>
public sealed class Parser
{
    private readonly List<Token> _t;
    private int _p;

    public Parser(List<Token> tokens) => _t = tokens;

    private Token Cur => _t[_p];
    private Token Next() => _t[_p++];
    private bool Is(TokenType t) => Cur.Type == t;

    private Token Expect(TokenType t, string what)
    {
        if (!Is(t)) throw new CalcException($"Expected {what}.");
        return Next();
    }

    /// <summary>Parses a whole line, including a trailing <c>→ VAR</c> store.</summary>
    public Node ParseProgram()
    {
        Node node = ParseExpression();

        // "expr → A"  /  "expr -> A"  /  "expr STO A"
        if (IsKeyword("sto"))
        {
            Next();
            node = new AssignNode(node, ReadVarName());
        }

        if (!Is(TokenType.End)) throw new CalcException("SYNTAX ERROR");
        return node;
    }

    private bool IsKeyword(string kw) =>
        Is(TokenType.Identifier) && string.Equals(Cur.Text, kw, StringComparison.OrdinalIgnoreCase);

    private string ReadVarName()
    {
        var id = Expect(TokenType.Identifier, "a variable name").Text;
        return id;
    }

    // additive := multiplicative (('+' | '-') multiplicative)*
    private Node ParseExpression()
    {
        Node left = ParseMultiplicative();
        while (Is(TokenType.Plus) || Is(TokenType.Minus))
        {
            char op = Next().Type == TokenType.Plus ? '+' : '-';
            left = new BinaryNode(op, left, ParseMultiplicative());
        }
        return left;
    }

    // multiplicative := unary (('*' | '/' | juxtaposition) unary)*
    private Node ParseMultiplicative()
    {
        Node left = ParseUnary();
        while (true)
        {
            if (Is(TokenType.Star)) { Next(); left = new BinaryNode('*', left, ParseUnary()); }
            else if (Is(TokenType.Slash)) { Next(); left = new BinaryNode('/', left, ParseUnary()); }
            else if (Is(TokenType.Percent)) { Next(); left = new BinaryNode('/', left, new NumberNode(100)); }
            else if (IsKeyword("mod")) { Next(); left = new CallNode("mod", new Node[] { left, ParseUnary() }); }
            else if (StartsImplicitFactor())
                left = new BinaryNode('*', left, ParseUnary());
            else break;
        }
        return left;
    }

    // A new value with no operator in front means "times" — but a reserved word
    // (STO, mod) is not the start of a factor.
    private bool StartsImplicitFactor()
    {
        if (Is(TokenType.Number) || Is(TokenType.LParen)) return true;
        if (Is(TokenType.Identifier))
            return !IsKeyword("sto") && !IsKeyword("mod");
        return false;
    }

    // unary := ('+' | '-') unary | power
    private Node ParseUnary()
    {
        if (Is(TokenType.Minus)) { Next(); return new UnaryNode('-', ParseUnary()); }
        if (Is(TokenType.Plus)) { Next(); return ParseUnary(); }
        return ParsePower();
    }

    // power := postfix ('^' unary)?     (right-associative, exponent may be signed)
    private Node ParsePower()
    {
        Node baseNode = ParsePostfix();
        if (Is(TokenType.Caret))
        {
            Next();
            return new BinaryNode('^', baseNode, ParseUnary());
        }
        return baseNode;
    }

    // postfix := primary '!'*
    private Node ParsePostfix()
    {
        Node node = ParsePrimary();
        while (Is(TokenType.Bang)) { Next(); node = new FactorialNode(node); }
        return node;
    }

    private Node ParsePrimary()
    {
        if (Is(TokenType.Number)) return new NumberNode(Next().Number);

        if (Is(TokenType.LParen))
        {
            Next();
            Node inner = ParseExpression();
            CloseParen();
            return inner;
        }

        if (Is(TokenType.Identifier))
        {
            string name = Next().Text;

            if (Is(TokenType.LParen) && Functions.IsFunction(name))
            {
                Next();
                var args = new List<Node>();
                if (!Is(TokenType.RParen))
                {
                    args.Add(ParseExpression());
                    while (Is(TokenType.Comma)) { Next(); args.Add(ParseExpression()); }
                }
                CloseParen();
                return new CallNode(name, args);
            }

            // not a function → it's a variable / constant; a following '(' is
            // handled by ParseMultiplicative as juxtaposition (x(2) = x*2).
            return new VariableNode(name);
        }

        throw new CalcException("SYNTAX ERROR");
    }

    /// <summary>
    /// Consume a ')'. A missing ')' at the end of the input is tolerated —
    /// <c>nCr(10,2</c> and <c>sin(x</c> evaluate as if the brackets were closed,
    /// the way a TI-84 auto-closes trailing parens on ENTER.
    /// </summary>
    private void CloseParen()
    {
        if (Is(TokenType.RParen)) { Next(); return; }
        if (Is(TokenType.End)) return;
        throw new CalcException("Expected ')'.");
    }
}
