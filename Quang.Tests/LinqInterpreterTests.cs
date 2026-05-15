using Quang.Interpreters;

namespace Quang.Tests;

public class TestUser
{
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class LinqInterpreterTests
{
    private Func<TestUser, bool> CompileQuang(string input)
    {
        var quang = new Quang(input)
            .SyntaxExpectSymbol("Age", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("Name", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("IsActive", new ExpressionValueTypeInfo<BoolExpression>())
            .Init();

        var translator = new LinqInterpreter<TestUser>();

        var lambda = translator.Translate(quang);

        return lambda.Compile();
    }

    [Fact]
    public void Translate_BasicComparisons_ReturnsExpectedResults()
    {
        var test = "Age gte 18";
        var func = CompileQuang(test);

        Assert.True(func(new TestUser { Age = 20 }));
        Assert.True(func(new TestUser { Age = 18 }));
        Assert.False(func(new TestUser { Age = 17 }));
    }

    [Fact]
    public void Translate_LogicalAnd_EvaluatesBothSides()
    {
        var test = "Age gte 18 and IsActive eq true";
        var func = CompileQuang(test);

        Assert.True(func(new TestUser { Age = 20, IsActive = true }));
        Assert.False(func(new TestUser { Age = 17, IsActive = true }));
        Assert.False(func(new TestUser { Age = 20, IsActive = false }));
    }

    [Fact]
    public void Translate_UnaryNot_NegatesExpression()
    {
        var test = "not (IsActive eq true)"; // Parses as Not(Eq(IsActive, true))
        var func = CompileQuang(test);

        Assert.True(func(new TestUser { IsActive = false }));
        Assert.False(func(new TestUser { IsActive = true }));
    }

    [Fact]
    public void Translate_NotWithParentheses_NegatesEntireGroup()
    {
        // Equivalent to: !(Age < 18 || IsActive == false)
        // Which implies: Age >= 18 AND IsActive == true
        var test = "not (Age lt 18 or IsActive eq false)";
        var func = CompileQuang(test);

        Assert.True(func(new TestUser { Age = 20, IsActive = true }));
        Assert.False(func(new TestUser { Age = 17, IsActive = true })); // Fails because it's < 18
        Assert.False(func(new TestUser { Age = 20, IsActive = false })); // Fails because Active is false
    }

    [Fact]
    public void Translate_DoubleNot_CancelsOut()
    {
        var test = "not (not (IsActive eq true))";
        var func = CompileQuang(test);

        Assert.True(func(new TestUser { IsActive = true }));
        Assert.False(func(new TestUser { IsActive = false }));
    }

    [Fact]
    public void Translate_RegOperator_CompilesToContains()
    {
        var test = "Name reg 'al'";
        var func = CompileQuang(test);

        Assert.True(func(new TestUser { Name = "Valeria" }));
        Assert.False(func(new TestUser { Name = "Alice" })); // "al" doesn't match "Al"
        Assert.False(func(new TestUser { Name = "Bob" }));
    }
}
