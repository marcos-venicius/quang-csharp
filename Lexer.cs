namespace Quang;

internal class Lexer
{
    private int _cursor, _bot;
    private readonly int _size;
    private readonly string _content;
    private readonly List<Token> _tokens = [];

    internal Lexer(string query)
    {
        _content = query;
        _cursor = 0;
        _bot = 0;
        _size = query.Length;
    }

    internal List<Token> Lex()
    {
        while (!IsEmpty())
        {
            TrimWhitespaces();

            if (IsEmpty()) break;

            _bot = _cursor;

            var chr = Char();

            switch (chr)
            {
                case '\'': LexString(); break;
                case '(': LexSingle(TokenKind.OpenParen); break;
                case ')': LexSingle(TokenKind.CloseParen); break;
                case ':': LexAtom(); break;
                default: {
                    if (IsDigit(chr)) LexNumber();
                    else if (IsSymbol(chr)) LexSymbolOrKeyword();
                    else throw new QuangSyntaxException($"unexpected character \"{chr}\"", _cursor+1);
                } break;
            }
        }

        return _tokens;
    }

    private bool IsSymbol(char chr) => chr == '_' || (chr >= 'a' && chr <= 'z') || (chr >= 'A' && chr <= 'Z');
    private bool IsDigit(char chr) => chr >= '0' && chr <= '9';
    private bool IsEmpty() => _cursor >= _size;
    private bool IsEmptyAhead() => _cursor + 1 >= _size;
    private char Char() => IsEmpty() ? '\0' : _content[_cursor];
    private char CharAhead() => IsEmptyAhead() ? '\0' : _content[_cursor + 1];
    private void AdvanceCursor()
    {
        if (!IsEmpty()) _cursor++;
    }

    private void TrimWhitespaces()
    {
        while (!IsEmpty() && Char() == ' ') AdvanceCursor();
    }

    private void LexString()
    {
        AdvanceCursor();

        while (!IsEmpty() && Char() != '\'')
        {
            if (Char() == '\\')
            {
                if (IsEmptyAhead())
                    throw new QuangSyntaxException("unterminated string literal", _bot + 1);

                switch (CharAhead())
                {
                    case '\'':
                    case '\\':
                        AdvanceCursor();
                        break;
                    default:
                        throw new QuangSyntaxException("invalid scape sequence", _cursor + 1);
                }
            }

            AdvanceCursor();
        }

        if (IsEmpty()) throw new QuangSyntaxException("unterminated string literal", _bot + 1);

        var token = new Token(_content[(_bot + 1) .. _cursor], TokenKind.String, _bot + 1);

        _tokens.Add(token);

        AdvanceCursor();
    }

    private void LexAtom()
    {
        AdvanceCursor();

        var atomNameSize = 0;

        while (!IsEmpty() && IsSymbol(Char()))
        {
            AdvanceCursor();
            atomNameSize++;
        }

        if (atomNameSize == 0)
            throw new QuangSyntaxException("missing atom name", _cursor);

        var token = new Token(_content[_bot .. _cursor], TokenKind.Atom, _bot + 1);

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

        var token = new Token(_content[_bot .. _cursor], isFloat ? TokenKind.Float : TokenKind.Integer, _bot + 1);

        _tokens.Add(token);
    }

    private void LexSymbolOrKeyword()
    {
        while (!IsEmpty() && IsSymbol(Char())) AdvanceCursor();

        var content = _content[_bot .. _cursor];
        var kind = Keywords.MatchKeywordOrSymbol(content);

        var token = new Token(content, kind, _bot + 1);

        _tokens.Add(token);
    }

    private void LexSingle(TokenKind kind)
    {
        AdvanceCursor();

        var token = new Token(_content[_bot .. _cursor], kind, _bot + 1);

        _tokens.Add(token);
    }
}
