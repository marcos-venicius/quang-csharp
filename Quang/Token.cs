namespace Quang;

internal class Token
{
    internal string Value { get; }
    internal TokenKind Kind { get; }
    internal int Col { get; }

    public Token(string value, TokenKind kind, int col)
    {
        Value = value;
        Kind = kind;
        Col = col;
    }
}

