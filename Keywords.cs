namespace Quang;

internal class Keywords
{
    private static readonly Dictionary<string, TokenKind> _keywords = new()
    {
        { "true",  TokenKind.TrueKeyword },
        { "false", TokenKind.FalseKeyword },
        { "nil",   TokenKind.NilKeyword },
        { "and",   TokenKind.AndKeyword },
        { "or",    TokenKind.OrKeyword },
        { "reg",   TokenKind.RegKeyword },
        { "eq",    TokenKind.EqKeyword },
        { "ne",    TokenKind.NeKeyword },
        { "gt",    TokenKind.GtKeyword },
        { "lt",    TokenKind.LtKeyword },
        { "gte",   TokenKind.GteKeyword },
        { "lte",   TokenKind.LteKeyword },
    };

    internal static TokenKind MatchKeywordOrSymbol(string symbol)
    {
        if (_keywords.ContainsKey(symbol)) return _keywords[symbol];

        return TokenKind.Symbol;
    }
}
