using System.Globalization;
using System.Text;

namespace Quang;

/// <summary>
/// Grammar:
///
///   expression := term ('or' term)*
///   term       := factor ('and' factor)*
///   factor     := 'not' factor | comparison
///   comparison := primary (cmp_op primary)?
///   primary    := '(' expression ')' | literal | symbol
///
/// 'not' sits below the comparison, so "not status eq 400" means "not (status eq 400)".
/// </summary>
internal class Parser
{
    private readonly List<Token> _tokens;
    private int _cursor;
    private readonly int _size;

    internal Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _cursor = 0;
        _size = tokens.Count;
    }

    /// <summary>
    /// Parses the whole token stream. Returns null when the query is empty.
    /// Every token must be consumed, otherwise the query is malformed.
    /// </summary>
    internal Expression? Parse()
    {
        if (IsEmpty()) return null;

        var expression = ParseExpression();

        if (!IsEmpty())
        {
            var token = Token();

            throw new QuangSyntaxException($"unexpected token \"{token.Value}\"", token.Line, token.Col);
        }

        return expression;
    }

    internal Expression ParseExpression()
    {
        var left = ParseTerm();

        while (!IsEmpty())
        {
            var current = Token();

            if (current.Kind != TokenKind.OrKeyword) break;

            AdvanceCursor();

            if (IsEmpty())
                throw new QuangSyntaxException("expected an expression after 'or'", current.Line, current.Col);

            var right = ParseTerm();

            left = new BinaryExpression(left, BinaryOperator.Or, right);
        }

        return left;
    }

    internal Expression ParseTerm()
    {
        var left = ParseFactor();

        while (!IsEmpty())
        {
            var current = Token();

            if (current.Kind != TokenKind.AndKeyword) break;

            AdvanceCursor();

            if (IsEmpty())
                throw new QuangSyntaxException("expected an expression after 'and'", current.Line, current.Col);

            var right = ParseFactor();

            left = new BinaryExpression(left, BinaryOperator.And, right);
        }

        return left;
    }

    private Expression ParseFactor()
    {
        if (!IsEmpty() && Token().Kind == TokenKind.NotKeyword)
        {
            var current = Token();

            AdvanceCursor();

            if (IsEmpty())
                throw new QuangSyntaxException("expected an expression after 'not'", current.Line, current.Col);

            return new UnaryExpression(ParseFactor(), UnaryOperator.Not);
        }

        return ParseComparison();
    }

    internal Expression ParseComparison()
    {
        var left = ParsePrimary();

        if (IsEmpty()) return left;

        var current = Token();

        switch (current.Kind)
        {
            case TokenKind.EqKeyword:
            case TokenKind.NeKeyword:
            case TokenKind.GtKeyword:
            case TokenKind.LtKeyword:
            case TokenKind.GteKeyword:
            case TokenKind.LteKeyword:
            case TokenKind.RegKeyword:
                AdvanceCursor();

                if (IsEmpty())
                    throw new QuangSyntaxException($"expected an expression after '{current.Value}'", current.Line, current.Col);

                var right = ParsePrimary();

                var op = current.Kind switch
                {
                    TokenKind.EqKeyword => BinaryOperator.Eq,
                    TokenKind.NeKeyword => BinaryOperator.Ne,
                    TokenKind.GtKeyword => BinaryOperator.Gt,
                    TokenKind.LtKeyword => BinaryOperator.Lt,
                    TokenKind.GteKeyword => BinaryOperator.Gte,
                    TokenKind.LteKeyword => BinaryOperator.Lte,
                    TokenKind.RegKeyword => BinaryOperator.Reg,
                    _ => throw new QuangSyntaxException($"unexpected token \"{current.Value}\"", current.Line, current.Col),
                };

                return new BinaryExpression(left, op, right);
            case TokenKind.OrKeyword:
            case TokenKind.AndKeyword:
            case TokenKind.CloseParen:
                return left;
            default:
                throw new QuangSyntaxException($"expected comparison operator after expression but got \"{current.Value}\"", current.Line, current.Col);
        }
    }

    private Expression ParsePrimary()
    {
        if (IsEmpty())
        {
            var last = Last();

            throw new QuangSyntaxException("unexpected end of the query", last.Line, last.Col);
        }

        var current = Token();

        if (current.Kind == TokenKind.OpenParen)
        {
            AdvanceCursor();

            if (IsEmpty()) throw new QuangSyntaxException("missing ')'", current.Line, current.Col);

            var expr = ParseExpression();

            if (IsEmpty()) throw new QuangSyntaxException("missing ')'", Last().Line, Last().Col);

            var close = Token();

            if (close.Kind != TokenKind.CloseParen)
                throw new QuangSyntaxException($"expected ')' but got \"{close.Value}\"", close.Line, close.Col);

            AdvanceCursor();

            return expr;
        }

        AdvanceCursor();

        return current.Kind switch
        {
            TokenKind.Integer => ParseInteger(current),
            TokenKind.Float => ParseFloat(current),
            TokenKind.TrueKeyword => new BoolExpression(true),
            TokenKind.FalseKeyword => new BoolExpression(false),
            TokenKind.NilKeyword => new NilExpression(),
            TokenKind.Atom => new AtomExpression(new Atom(current.Value)),
            TokenKind.Symbol => new SymbolExpression(current.Value),
            TokenKind.String => new StringExpression(UnescapeString(current.Value)),
            _ => throw new QuangSyntaxException($"unexpected token \"{current.Value}\"", current.Line, current.Col),
        };
    }

    // Literals are always parsed with the invariant culture, otherwise "1.5" would mean 15
    // on any machine where the decimal separator is a comma.
    private static IntegerExpression ParseInteger(Token token)
    {
        if (!long.TryParse(token.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            throw new QuangSyntaxException($"integer literal \"{token.Value}\" is out of range", token.Line, token.Col);

        return new IntegerExpression(value);
    }

    private static FloatExpression ParseFloat(Token token)
    {
        if (!double.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            throw new QuangSyntaxException($"float literal \"{token.Value}\" is out of range", token.Line, token.Col);

        return new FloatExpression(value);
    }

    private bool IsEmpty() => _cursor >= _size;
    private Token Token() => _tokens[_cursor];
    private Token Last() => _tokens[_size - 1];
    private void AdvanceCursor()
    {
        if (!IsEmpty()) _cursor++;
    }

    // Only "\'" and "\\" are escape sequences. Any other backslash is part of the string,
    // which keeps regex patterns like 'ML-\d+' readable.
    private static string UnescapeString(string text)
    {
        var sb = new StringBuilder();

        int i = 0;

        while (i < text.Length)
        {
            if (text[i] == '\\' && i + 1 < text.Length && (text[i + 1] == '\'' || text[i + 1] == '\\'))
                i++;

            sb.Append(text[i]);

            i++;
        }

        return sb.ToString();
    }
}
