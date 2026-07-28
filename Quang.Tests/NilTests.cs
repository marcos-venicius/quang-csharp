namespace Quang.Tests;

public class NilTests
{
    private static Evaluator Build(string query)
    {
        var expr = new Parser(new Lexer(query).Lex()).Parse();

        return new Evaluator(expr, []);
    }

    [Theory]
    [InlineData("nil eq nil", true)]
    [InlineData("nil ne nil", false)]
    [InlineData("'' eq nil", true)]
    [InlineData("'' ne nil", false)]
    [InlineData("nil eq ''", true)]
    [InlineData("'x' eq nil", false)]
    [InlineData("'x' ne nil", true)]
    [InlineData("0 eq nil", false)]
    [InlineData("0 ne nil", true)]
    [InlineData("0.0 eq nil", false)]
    [InlineData("false eq nil", false)]
    public void Evaluate_NilLiterals_FollowsTheEmptyValueSemantics(string query, bool expected)
    {
        Assert.Equal(expected, Build(query).Evaluate());
    }

    [Fact]
    public void Evaluate_NilVariables_ReturnsExpectedResults()
    {
        var evaluator = Build("name eq nil");

        evaluator.AddNilVar("name");
        Assert.True(evaluator.Evaluate());

        evaluator.AddStringVar("name", null);
        Assert.True(evaluator.Evaluate());

        evaluator.AddStringVar("name", "");
        Assert.True(evaluator.Evaluate());

        evaluator.AddStringVar("name", "marcos");
        Assert.False(evaluator.Evaluate());

        evaluator.AddIntegerVar("name", (long?)null);
        Assert.True(evaluator.Evaluate());

        evaluator.AddIntegerVar("name", 0);
        Assert.False(evaluator.Evaluate());
    }

    [Fact]
    public void Evaluate_NilAgainstAPattern_IsFalse()
    {
        var evaluator = Build("name reg 'ma'");

        evaluator.AddNilVar("name");
        Assert.False(evaluator.Evaluate());

        evaluator.AddStringVar("name", null);
        Assert.False(evaluator.Evaluate());

        evaluator.AddStringVar("name", "marcos");
        Assert.True(evaluator.Evaluate());
    }

    [Fact]
    public void Evaluate_NilWithOrderedOperators_Throws()
    {
        var evaluator = Build("name lt nil");

        evaluator.AddStringVar("name", "marcos");

        var ex = Assert.Throws<QuangEvaluationException>(() => evaluator.Evaluate());

        Assert.Equal("error: you cannot do such operation 'string lt nil'", ex.Message);
    }

    [Fact]
    public void Evaluate_NilThroughThePublicApi_ReturnsExpectedResults()
    {
        var quang = new Quang("username eq nil")
            .Init()
            .SyntaxExpectSymbol("username", new ExpressionValueTypeInfo<StringExpression>());

        var evaluator = quang.Evaluator();

        evaluator.AddStringVar("username", null);
        Assert.True(evaluator.Evaluate());

        evaluator.AddStringVar("username", "user001");
        Assert.False(evaluator.Evaluate());
    }
}
