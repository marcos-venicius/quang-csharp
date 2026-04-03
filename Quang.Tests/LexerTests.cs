namespace Quang.Tests;

public class LexerTests
{
    [Fact]
    public void Lex_ParenthesisSequence_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("())(");

        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.Equal(4, tokens.Count);

        Assert.Equal(TokenKind.OpenParen, tokens[0].Kind);
        Assert.Equal(TokenKind.CloseParen, tokens[1].Kind);
        Assert.Equal(TokenKind.CloseParen, tokens[2].Kind);
        Assert.Equal(TokenKind.OpenParen, tokens[3].Kind);

        Assert.Equal("(", tokens[0].Value);
        Assert.Equal(")", tokens[1].Value);
        Assert.Equal(")", tokens[2].Value);
        Assert.Equal("(", tokens[3].Value);
    }

    [Fact]
    public void Lex_Integers_ReturnsCorrectTokens()
    {
        // Arrange
        var lexer = new Lexer("0 124");

        // Act
        var tokens = lexer.Lex();

        // Assert
        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);

        Assert.Equal(TokenKind.Integer, tokens[0].Kind);
        Assert.Equal(TokenKind.Integer, tokens[1].Kind);

        Assert.Equal("0", tokens[0].Value);
        Assert.Equal("124", tokens[1].Value);
    }

    [Fact]
    public void Lex_Floats_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("0. 0.1 3.1415");

        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.Equal(3, tokens.Count);

        Assert.Equal(TokenKind.Float, tokens[0].Kind);
        Assert.Equal(TokenKind.Float, tokens[1].Kind);
        Assert.Equal(TokenKind.Float, tokens[2].Kind);

        Assert.Equal("0.", tokens[0].Value);
        Assert.Equal("0.1", tokens[1].Value);
        Assert.Equal("3.1415", tokens[2].Value);
    }

    [Fact]
    public void Lex_Keywords_ReturnsCorrectTokens()
    {
        var input = string.Join(" ", Keywords.Mappings.Keys);
        var lexer = new Lexer(input);

        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.Equal(Keywords.Mappings.Count, tokens.Count);

        foreach (var token in tokens)
        {
            var expectedKind = Keywords.Mappings[token.Value];
            Assert.Equal(expectedKind, token.Kind);
        }
    }

    [Fact]
    public void Lex_Symbols_ReturnsCorrectTokens()
    {
        var lexer = new Lexer("hello guys");

        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.Equal(2, tokens.Count);

        Assert.Equal("hello", tokens[0].Value);
        Assert.Equal("guys", tokens[1].Value);

        Assert.Equal(TokenKind.Symbol, tokens[0].Kind);
        Assert.Equal(TokenKind.Symbol, tokens[1].Kind);
    }

    [Fact]
    public void Lex_Atoms_ReturnsCorrectTokens_AndHandlesErrors()
    {
        // Valid atoms
        var lexer = new Lexer(":hello_world :_ :h :hi");
        var tokens = lexer.Lex();

        Assert.NotNull(tokens);
        Assert.Equal(4, tokens.Count);

        Assert.Equal(":hello_world", tokens[0].Value);
        Assert.Equal(":_", tokens[1].Value);
        Assert.Equal(":h", tokens[2].Value);
        Assert.Equal(":hi", tokens[3].Value);

        Assert.Equal(TokenKind.Atom, tokens[0].Kind);
        Assert.Equal(TokenKind.Atom, tokens[1].Kind);
        Assert.Equal(TokenKind.Atom, tokens[2].Kind);
        Assert.Equal(TokenKind.Atom, tokens[3].Kind);

        // Invalid atom (just ":")
        var invalidLexer = new Lexer(":");

        var exception = Assert.Throws<QuangSyntaxException>(() => invalidLexer.Lex());
        Assert.Equal("error 1:1: missing atom name", exception.Message);
    }

    [Fact]
    public void Lex_Strings_ReturnsCorrectTokens_AndHandlesErrors()
    {
        // Simple string
        var lexer = new Lexer("'Hello World'");
        var tokens = lexer.Lex();
        Assert.NotNull(tokens);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.String, tokens[0].Kind);
        Assert.Equal("Hello World", tokens[0].Value);

        // String with escaped single quote
        lexer = new Lexer("'Hello \\'World\\''");
        tokens = lexer.Lex();
        Assert.NotNull(tokens);
        Assert.Single(tokens);
        Assert.Equal(TokenKind.String, tokens[0].Kind);
        Assert.Equal("Hello \\'World\\'", tokens[0].Value);

        // Unterminated strings
        lexer = new Lexer("'Hello \\");
        var ex1 = Assert.Throws<QuangSyntaxException>(() => lexer.Lex());
        Assert.Equal("error 1:1: unterminated string literal", ex1.Message);

        lexer = new Lexer("'Hello \\'");
        var ex2 = Assert.Throws<QuangSyntaxException>(() => lexer.Lex());
        Assert.Equal("error 1:1: unterminated string literal", ex2.Message);

        lexer = new Lexer("   'Hello s    sdflkjsdf sdlkjsdf\\'sdflksdfj");
        var ex3 = Assert.Throws<QuangSyntaxException>(() => lexer.Lex());
        Assert.Equal("error 1:4: unterminated string literal", ex3.Message);
    }
}
