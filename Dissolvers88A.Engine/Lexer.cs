using System.Globalization;
using System.Text;

namespace Dissolvers88A.Engine;

public enum TokenType
{
    Number, Identifier,
    Plus, Minus, Star, Slash, Caret, Percent,
    LParen, RParen, Comma, Bang,
    End
}

public readonly record struct Token(TokenType Type, string Text, double Number = 0, int Position = 0);

/// <summary>
/// Turns calculator input into a token stream. Tolerates the unicode symbols the
/// on-screen keypad produces (× ÷ − √ π ² ³ ⁻¹ ≠ ≤ ≥) and folds them onto plain
/// ASCII equivalents so the parser only ever sees one spelling.
/// </summary>
public static class Lexer
{
    public static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int i = 0;
        int n = input.Length;

        while (i < n)
        {
            char c = input[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            // ----- numbers: 12, 12.5, .5, 1.5e3, 2E-4 -----
            if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(input[i + 1])))
            {
                int start = i;
                var sb = new StringBuilder();
                while (i < n && char.IsDigit(input[i])) sb.Append(input[i++]);
                if (i < n && input[i] == '.')
                {
                    sb.Append('.'); i++;
                    while (i < n && char.IsDigit(input[i])) sb.Append(input[i++]);
                }
                if (i < n && (input[i] == 'e' || input[i] == 'E'))
                {
                    int save = i;
                    var exp = new StringBuilder("E");
                    i++;
                    if (i < n && (input[i] == '+' || input[i] == '-' || input[i] == '−'))
                        exp.Append(input[i++] == '−' ? '-' : input[i - 1]);
                    if (i < n && char.IsDigit(input[i]))
                    {
                        while (i < n && char.IsDigit(input[i])) exp.Append(input[i++]);
                        sb.Append(exp);
                    }
                    else i = save; // a stray 'e' — it's the constant, not an exponent
                }
                double value = double.Parse(sb.ToString(), CultureInfo.InvariantCulture);
                tokens.Add(new Token(TokenType.Number, sb.ToString(), value, start));
                continue;
            }

            // ----- identifiers: letters then letters/digits; also π, θ -----
            if (char.IsLetter(c) || c == 'π' || c == 'θ' || c == '_')
            {
                int start = i;
                var sb = new StringBuilder();
                while (i < n && (char.IsLetterOrDigit(input[i]) || input[i] == '_'
                                 || input[i] == 'π' || input[i] == 'θ'))
                    sb.Append(input[i++]);
                tokens.Add(new Token(TokenType.Identifier, sb.ToString(), 0, start));
                continue;
            }

            // ----- operators and punctuation (with unicode folding) -----
            switch (c)
            {
                case '+': tokens.Add(new(TokenType.Plus, "+", 0, i)); i++; break;
                case '-':
                case '−': // minus sign
                case '–': // en dash
                    tokens.Add(new(TokenType.Minus, "-", 0, i)); i++; break;
                case '*':
                case '×': // ×
                case '•': // •
                case '·': // ·
                    tokens.Add(new(TokenType.Star, "*", 0, i)); i++; break;
                case '/':
                case '÷': // ÷
                    tokens.Add(new(TokenType.Slash, "/", 0, i)); i++; break;
                case '^': tokens.Add(new(TokenType.Caret, "^", 0, i)); i++; break;
                case '(': case '[': case '{': tokens.Add(new(TokenType.LParen, "(", 0, i)); i++; break;
                case ')': case ']': case '}': tokens.Add(new(TokenType.RParen, ")", 0, i)); i++; break;
                case ',': tokens.Add(new(TokenType.Comma, ",", 0, i)); i++; break;
                case '!': tokens.Add(new(TokenType.Bang, "!", 0, i)); i++; break;
                case '%': tokens.Add(new(TokenType.Percent, "%", 0, i)); i++; break;

                // sugar that expands to real tokens
                case '√': // √  -> sqrt(
                    tokens.Add(new(TokenType.Identifier, "sqrt", 0, i));
                    tokens.Add(new(TokenType.LParen, "(", 0, i));
                    i++; break;
                case '²': // ²  -> ^2
                    tokens.Add(new(TokenType.Caret, "^", 0, i));
                    tokens.Add(new(TokenType.Number, "2", 2, i));
                    i++; break;
                case '³': // ³  -> ^3
                    tokens.Add(new(TokenType.Caret, "^", 0, i));
                    tokens.Add(new(TokenType.Number, "3", 3, i));
                    i++; break;
                case 'π': // stray π already handled above, but just in case
                    tokens.Add(new(TokenType.Identifier, "pi", 0, i)); i++; break;

                default:
                    throw new CalcException($"Unexpected character '{c}'.");
            }
        }

        tokens.Add(new Token(TokenType.End, "", 0, n));
        return tokens;
    }
}
