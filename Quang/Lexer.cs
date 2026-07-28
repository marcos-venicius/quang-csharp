namespace Quang;

internal class Lexer
{
    private int _cursor, _bot;
    private int _line, _lineStart;
    private int _botLine, _botCol;
    private readonly int _size;
    private readonly string _content;
    private readonly List<Token> _tokens = [];

    internal Lexer(string query)
    {
        _content = query;
        _cursor = 0;
        _bot = 0;
        _line = 1;
        _lineStart = 0;
        _size = query.Length;
    }

    internal List<Token> Lex()
    {
        while (!IsEmpty())
        {
            TrimWhitespaces();

            if (IsEmpty()) break;

            _bot = _cursor;
            _botLine = _line;
            _botCol = Col();

            var chr = Char();

            switch (chr)
            {
                case '\'': LexString(); break;
                case '(': LexSingle(TokenKind.OpenParen); break;
                case ')': LexSingle(TokenKind.CloseParen); break;
                case ':': LexAtom(); break;
                default: {
                    if (IsDigit(chr)) LexNumber();
                    else if (IsSymbolStart(chr)) LexSymbolOrKeyword();
                    else throw new QuangSyntaxException($"unexpected character \"{chr}\"", _line, Col());
                } break;
            }
        }

        return _tokens;
    }

    private static bool IsSymbolStart(char chr) => chr == '_' || (chr >= 'a' && chr <= 'z') || (chr >= 'A' && chr <= 'Z');
    private static bool IsSymbolPart(char chr) => IsSymbolStart(chr) || IsDigit(chr);
    private static bool IsDigit(char chr) => chr >= '0' && chr <= '9';
    private bool IsEmpty() => _cursor >= _size;
    private bool IsEmptyAhead() => _cursor + 1 >= _size;
    private char Char() => IsEmpty() ? '\0' : _content[_cursor];
    private int Col() => _cursor - _lineStart + 1;

    private void AdvanceCursor()
    {
        if (IsEmpty()) return;

        if (_content[_cursor] == '\n')
        {
            _line++;
            _lineStart = _cursor + 1;
        }

        _cursor++;
    }

    private void TrimWhitespaces()
    {
        while (!IsEmpty() && char.IsWhiteSpace(Char())) AdvanceCursor();
    }

    private void LexString()
    {
        AdvanceCursor();

        while (!IsEmpty() && Char() != '\'')
        {
            // "\'" and "\\" are escapes, anything else after a backslash is kept as it is,
            // so regex patterns like 'ML-\d+' can be written without doubling the backslash.
            if (Char() == '\\')
            {
                if (IsEmptyAhead())
                    throw new QuangSyntaxException("unterminated string literal", _botLine, _botCol);

                AdvanceCursor();
            }

            AdvanceCursor();
        }

        if (IsEmpty()) throw new QuangSyntaxException("unterminated string literal", _botLine, _botCol);

        var token = new Token(_content[(_bot + 1) .. _cursor], TokenKind.String, _botLine, _botCol);

        _tokens.Add(token);

        AdvanceCursor();
    }

    private void LexAtom()
    {
        AdvanceCursor();

        var atomNameSize = 0;

        while (!IsEmpty() && (atomNameSize == 0 ? IsSymbolStart(Char()) : IsSymbolPart(Char())))
        {
            AdvanceCursor();
            atomNameSize++;
        }

        if (atomNameSize == 0)
            throw new QuangSyntaxException("missing atom name", _botLine, _botCol);

        var token = new Token(_content[_bot .. _cursor], TokenKind.Atom, _botLine, _botCol);

        _tokens.Add(token);
    }

    private void LexNumber()
    {
        while (!IsEmpty() && IsDigit(Char())) AdvanceCursor();

        var isFloat = false;

        if (Char() == '.') {
            isFloat = true;

            AdvanceCursor();

            while (!IsEmpty() && IsDigit(Char())) AdvanceCursor();
        }

        var token = new Token(_content[_bot .. _cursor], isFloat ? TokenKind.Float : TokenKind.Integer, _botLine, _botCol);

        _tokens.Add(token);
    }

    private void LexSymbolOrKeyword()
    {
        while (!IsEmpty() && IsSymbolPart(Char())) AdvanceCursor();

        var content = _content[_bot .. _cursor];
        var kind = Keywords.MatchKeywordOrSymbol(content);

        var token = new Token(content, kind, _botLine, _botCol);

        _tokens.Add(token);
    }

    private void LexSingle(TokenKind kind)
    {
        AdvanceCursor();

        var token = new Token(_content[_bot .. _cursor], kind, _botLine, _botCol);

        _tokens.Add(token);
    }
}
