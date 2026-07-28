using Quang.Interpreters;

namespace Quang.Tests;

public class TestUser
{
    public int Age { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public enum Sex
{
    F,
    M
}

public class TestAccount
{
    public long Id { get; set; }
    public double Weight { get; set; }
    public decimal Balance { get; set; }
    public int? Score { get; set; }
    public string? Nickname { get; set; }
    public bool? Verified { get; set; }
    public Sex Sex { get; set; }
}

public class LinqInterpreterTests
{
    private static Func<TestUser, bool> CompileQuang(string input, RegStrategy regStrategy = RegStrategy.Regex)
    {
        var quang = new Quang(input)
            .SyntaxExpectSymbol("Age", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("Name", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("IsActive", new ExpressionValueTypeInfo<BoolExpression>())
            .Init();

        var translator = new LinqInterpreter<TestUser>(regStrategy: regStrategy);

        return translator.Translate(quang).Compile();
    }

    private static Func<TestAccount, bool> CompileAccount(string input)
    {
        var quang = new Quang(input)
            .SyntaxExpectAtom(":m")
            .SyntaxExpectAtom(":f")
            .SyntaxExpectSymbol("Id", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("Weight", new ExpressionValueTypeInfo<FloatExpression>())
            .SyntaxExpectSymbol("Balance", new ExpressionValueTypeInfo<FloatExpression>())
            .SyntaxExpectSymbol("Score", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("Nickname", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("Verified", new ExpressionValueTypeInfo<BoolExpression>())
            .SyntaxExpectSymbol("Sex", new ExpressionValueTypeInfo<AtomExpression>())
            .Init();

        var translator = new LinqInterpreter<TestAccount>(
            atomsMapping: new Dictionary<string, string> { { ":m", "M" }, { ":f", "F" } });

        return translator.Translate(quang).Compile();
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
    public void Translate_EmptyQuery_MatchesEverything()
    {
        var func = CompileQuang("");

        Assert.True(func(new TestUser()));
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
    public void Translate_NotWithoutParentheses_NegatesTheComparison()
    {
        var func = CompileQuang("not Age lt 18");

        Assert.True(func(new TestUser { Age = 20 }));
        Assert.False(func(new TestUser { Age = 17 }));
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
    public void Translate_BooleanField_CanBeUsedDirectly()
    {
        var func = CompileQuang("IsActive and Age gt 10");

        Assert.True(func(new TestUser { Age = 20, IsActive = true }));
        Assert.False(func(new TestUser { Age = 20, IsActive = false }));

        var negated = CompileQuang("not IsActive");

        Assert.True(negated(new TestUser { IsActive = false }));
        Assert.False(negated(new TestUser { IsActive = true }));
    }

    [Fact]
    public void Translate_RegOperator_UsesRegexByDefault()
    {
        var func = CompileQuang("Name reg '^Al'");

        Assert.True(func(new TestUser { Name = "Alice" }));
        Assert.False(func(new TestUser { Name = "Valeria" })); // "Al" is not at the beginning
        Assert.False(func(new TestUser { Name = "Bob" }));
    }

    [Fact]
    public void Translate_RegOperator_CanCompileToContains()
    {
        var func = CompileQuang("Name reg 'al'", RegStrategy.Contains);

        Assert.True(func(new TestUser { Name = "Valeria" }));
        Assert.False(func(new TestUser { Name = "Alice" })); // "al" doesn't match "Al"
        Assert.False(func(new TestUser { Name = "Bob" }));

        // a regex pattern is a plain substring when the Contains strategy is used
        var pattern = CompileQuang("Name reg '^Al'", RegStrategy.Contains);

        Assert.False(pattern(new TestUser { Name = "Alice" }));
    }

    [Fact]
    public void Translate_LiteralOnTheLeftSide_FlipsTheComparison()
    {
        var func = CompileQuang("18 lt Age");

        Assert.True(func(new TestUser { Age = 20 }));
        Assert.False(func(new TestUser { Age = 18 }));
    }

    [Theory]
    [InlineData("Id eq 10", true)]
    [InlineData("Id gt 10", false)]
    [InlineData("Weight lte 70.5", true)]
    [InlineData("Balance gt 100", true)]
    [InlineData("Score eq 42", true)]
    [InlineData("Sex eq :m", true)]
    [InlineData("Sex eq :f", false)]
    public void Translate_CoercesLiteralsToTheFieldType(string query, bool expected)
    {
        var func = CompileAccount(query);

        var account = new TestAccount
        {
            Id = 10,
            Weight = 70.2,
            Balance = 150.75m,
            Score = 42,
            Sex = Sex.M,
        };

        Assert.Equal(expected, func(account));
    }

    [Fact]
    public void Translate_NilComparisons_MatchEmptyValues()
    {
        var nickname = CompileAccount("Nickname eq nil");

        Assert.True(nickname(new TestAccount { Nickname = null }));
        Assert.True(nickname(new TestAccount { Nickname = "" }));
        Assert.False(nickname(new TestAccount { Nickname = "quang" }));

        var score = CompileAccount("Score ne nil");

        Assert.True(score(new TestAccount { Score = 1 }));
        Assert.False(score(new TestAccount { Score = null }));

        // a non nullable field is never empty
        var id = CompileAccount("Id eq nil");

        Assert.False(id(new TestAccount { Id = 0 }));
    }

    [Fact]
    public void Translate_NullableBooleanField_CanBeUsedDirectly()
    {
        var func = CompileAccount("Verified");

        Assert.True(func(new TestAccount { Verified = true }));
        Assert.False(func(new TestAccount { Verified = false }));
        Assert.False(func(new TestAccount { Verified = null }));
    }

    [Fact]
    public void Translate_NestedExpressionComparedAsBoolean_Works()
    {
        var func = CompileQuang("(Age gt 18 or IsActive) eq true");

        Assert.True(func(new TestUser { Age = 20, IsActive = false }));
        Assert.True(func(new TestUser { Age = 10, IsActive = true }));
        Assert.False(func(new TestUser { Age = 10, IsActive = false }));
    }

    [Fact]
    public void Translate_UnknownField_ThrowsAQuangError()
    {
        var quang = new Quang("Missing eq 1")
            .SyntaxExpectSymbol("Missing", new ExpressionValueTypeInfo<IntegerExpression>())
            .Init();

        var ex = Assert.Throws<QuangEvaluationException>(() => new LinqInterpreter<TestUser>().Translate(quang));

        Assert.Contains("does not have a public property named 'Missing'", ex.Message);
    }

    [Fact]
    public void Translate_SymbolMapping_IsApplied()
    {
        var quang = new Quang("idade gte 18")
            .SyntaxExpectSymbol("idade", new ExpressionValueTypeInfo<IntegerExpression>())
            .Init();

        var translator = new LinqInterpreter<TestUser>(
            symbolsMapping: new Dictionary<string, string> { { "idade", "Age" } });

        var func = translator.Translate(quang).Compile();

        Assert.True(func(new TestUser { Age = 20 }));
        Assert.False(func(new TestUser { Age = 10 }));
    }
}
