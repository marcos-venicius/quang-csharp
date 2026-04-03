namespace Quang;

internal class Token
{
    private readonly string _value;
    private readonly TokenKind _kind;
    private readonly int _col;

    public Token(string value, TokenKind kind, int col)
    {
        _value = value;
        _kind = kind;
        _col = col;
    }
}

