namespace Quang.Tests;

public class ParserTests
{
    [Fact]
    public void ParseComparison_ReturnsCorrectExpression()
    {
        var data = "size eq 0";

        var tokens = new Lexer(data).Lex();
        var parser = new Parser(tokens);

        var expr = parser.ParseComparison();

        Assert.NotNull(expr);

        Assert.IsType<BinaryExpression>(expr);
        Assert.Equal(BinaryOperator.Eq, ((BinaryExpression)expr).Operator);

        Assert.IsType<SymbolExpression>(((BinaryExpression)expr).Left);
        Assert.Equal("size", ((SymbolExpression)((BinaryExpression)expr).Left).Symbol);

        Assert.IsType<IntegerExpression>(((BinaryExpression)expr).Right);
    }

    [Fact]
    public void ParseTerm_ReturnsCorrectExpression()
    {
        var data = "size gte 10.4 and method eq :post";

        var tokens = new Lexer(data).Lex();
        var parser = new Parser(tokens);

        var expr = parser.ParseTerm();

        Assert.NotNull(expr);

        Assert.IsType<BinaryExpression>(expr);
        Assert.Equal(BinaryOperator.And, ((BinaryExpression)expr).Operator);

        var left = ((BinaryExpression)expr).Left;

        Assert.IsType<BinaryExpression>(left);
        Assert.Equal(BinaryOperator.Gte, ((BinaryExpression)left).Operator);
        Assert.IsType<SymbolExpression>(((BinaryExpression)left).Left);
        Assert.IsType<FloatExpression>(((BinaryExpression)left).Right);

        var right = ((BinaryExpression)expr).Right;

        Assert.IsType<BinaryExpression>(right);
        Assert.Equal(BinaryOperator.Eq, ((BinaryExpression)right).Operator);
        Assert.IsType<SymbolExpression>(((BinaryExpression)right).Left);
        Assert.IsType<AtomExpression>(((BinaryExpression)right).Right);
        Assert.Equal(":post", ((AtomExpression)((BinaryExpression)right).Right).Atom);
    }

    [Fact]
    public void ParseExpression_ReturnsValidExpression_ForComplexAndBooleanCases()
    {
        var data = "(method eq :get and size gt 0 and size lte 1024) or (method eq :post and status ne 204) and true eq false or 10.5 lte 23.567 and name eq nil";

        var tokens = new Lexer(data).Lex();
        var parser = new Parser(tokens);

        var expr = parser.ParseExpression();

        Assert.NotNull(expr);

        var tests = new[]
        {
            "true or true",
            "true or false",
            "false or true",
            "false or false",
            "true and true",
            "true and false",
            "false and true",
            "false and false",
        };

        foreach (var test in tests)
        {
            var tkns = new Lexer(test).Lex();
            var p = new Parser(tkns);

            var e = p.ParseExpression();

            Assert.NotNull(e);
        }
    }
}