using System.Text;

namespace Quang;

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

    internal Expression? Parse()
    {
        if (IsEmpty()) return null;

        return ParseExpression();
    }

    internal Expression? ParseExpression()
    {
        if (IsEmpty()) return null;

        var left = ParseTerm();

        while (!IsEmpty())
        {
            var current = Token();

            if (current.Kind != TokenKind.OrKeyword) break;

            AdvanceCursor();

            var right = ParseTerm();

            left = new BinaryExpression(left!, BinaryOperator.Or, right!);
        }

        return left;
    }

    private Expression? ParsePrimary()
    {
        if (IsEmpty()) throw new QuangSyntaxException("missing token", Last().Col);

        var current = Token();

        AdvanceCursor();

        return current.Kind switch
        {
            TokenKind.Integer => new IntegerExpression(int.Parse(current.Value)),
            TokenKind.Float => new FloatExpression(float.Parse(current.Value)),
            TokenKind.TrueKeyword => new BoolExpression(true),
            TokenKind.FalseKeyword => new BoolExpression(false),
            TokenKind.NilKeyword => new NilExpression(),
            TokenKind.Atom => new AtomExpression((Atom)current.Value),
            TokenKind.Symbol => new SymbolExpression(current.Value),
            TokenKind.String => new StringExpression(UnescapeString(current.Value)),
            _ => throw new QuangSyntaxException($"unexpected token \"{current.Value}\"", current.Col),
        };
    }

    internal Expression? ParseComparison()
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
                    _ => throw new QuangSyntaxException($"unexpected token \"{current.Value}\"", current.Col),
                };

                return new BinaryExpression(left!, op, right!);
            case TokenKind.OrKeyword:
            case TokenKind.AndKeyword:
            case TokenKind.CloseParen:
                return left;
            default:
                throw new QuangSyntaxException($"expected comparison operator after expression but got \"{current.Value}\"", current.Col);
        }
    }

    private Expression? ParseFactor()
    {
        var current = Token();

        if (current.Kind == TokenKind.OpenParen)
        {
            AdvanceCursor();

            var expr = ParseExpression();

            current = Token();

            if (current.Kind != TokenKind.CloseParen)
                throw new QuangSyntaxException($"expected ')' but got \"{current.Value}\"", current.Col);

            AdvanceCursor();

            return expr;
        }

        return ParseComparison();
    }

    internal Expression? ParseTerm()
    {
        var left = ParseFactor();

        while (!IsEmpty())
        {
            var current = Token();

            if (current.Kind != TokenKind.AndKeyword) break;

            AdvanceCursor();

            var right = ParseFactor();

            left = new BinaryExpression(left!, BinaryOperator.And, right!);
        }

        return left;
    }

    private bool IsEmpty() => _cursor >= _size;
    private Token Token() => _tokens[_cursor];
    private Token Last() => _tokens[_size - 1];
    private void AdvanceCursor()
    {
        if (!IsEmpty()) _cursor++;
    }

    private static string UnescapeString(string text)
    {
        var sb = new StringBuilder();

        int i = 0;

        while (i < text.Length)
        {
            if (text[i] == '\\') i++;

            sb.Append(text[i]);

            i++;
        }

        return sb.ToString();
    }
}