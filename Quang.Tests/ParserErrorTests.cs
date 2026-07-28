namespace Quang.Tests;

public class ParserErrorTests
{
    private static Expression? Parse(string query) => new Parser(new Lexer(query).Lex()).Parse();

    [Fact]
    public void Parse_EmptyQuery_ReturnsNullAndEvaluatesToTrue()
    {
        Assert.Null(Parse(""));
        Assert.Null(Parse("   "));

        Assert.True(new Evaluator(Parse(""), []).Evaluate());
    }

    [Theory]
    // leftover tokens used to be silently ignored
    [InlineData("status eq 200 name eq 'x'", "unexpected token \"name\"")]
    [InlineData("1 eq 1 eq 2", "unexpected token \"eq\"")]
    [InlineData("(true))", "unexpected token \")\"")]
    [InlineData("true false", "expected comparison operator after expression but got \"false\"")]
    // missing operands used to blow up with a NullReferenceException
    [InlineData("true and", "expected an expression after 'and'")]
    [InlineData("true or", "expected an expression after 'or'")]
    [InlineData("status eq", "expected an expression after 'eq'")]
    [InlineData("not", "expected an expression after 'not'")]
    [InlineData("(true", "missing ')'")]
    [InlineData("(", "missing ')'")]
    [InlineData("99999999999999999999 eq 1", "integer literal \"99999999999999999999\" is out of range")]
    public void Parse_InvalidQueries_ThrowSyntaxErrors(string query, string expectedMessage)
    {
        var ex = Assert.Throws<QuangSyntaxException>(() => Parse(query));

        Assert.Contains(expectedMessage, ex.Message);
        Assert.NotNull(ex.Line);
        Assert.NotNull(ex.Column);
    }

    [Fact]
    public void Parse_Not_AppliesToTheWholeComparison()
    {
        var expr = Parse("not status eq 400");

        var unary = Assert.IsType<UnaryExpression>(expr);
        var binary = Assert.IsType<BinaryExpression>(unary.Expr);

        Assert.Equal(BinaryOperator.Eq, binary.Operator);
        Assert.Equal("status", Assert.IsType<SymbolExpression>(binary.Left).Value);
        Assert.Equal(400, Assert.IsType<IntegerExpression>(binary.Right).Value);
    }

    [Fact]
    public void Parse_Not_BindsTighterThanAnd()
    {
        var expr = Parse("not a eq 1 and b eq 2");

        var and = Assert.IsType<BinaryExpression>(expr);

        Assert.Equal(BinaryOperator.And, and.Operator);
        Assert.IsType<UnaryExpression>(and.Left);
        Assert.Equal(BinaryOperator.Eq, Assert.IsType<BinaryExpression>(and.Right).Operator);
    }

    [Fact]
    public void Parse_NumbersUseTheFullRange()
    {
        var expr = Parse("9223372036854775807 eq 1");

        var binary = Assert.IsType<BinaryExpression>(expr);

        Assert.Equal(long.MaxValue, Assert.IsType<IntegerExpression>(binary.Left).Value);
    }
}
