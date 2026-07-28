namespace Quang.Tests;

public class TypeCheckerTests
{
    private static Quang Schema(string query) =>
        new Quang(query)
            .Init()
            .SyntaxExpectAtom(":get")
            .SyntaxExpectSymbol("age", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("weight", new ExpressionValueTypeInfo<FloatExpression>())
            .SyntaxExpectSymbol("name", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("active", new ExpressionValueTypeInfo<BoolExpression>())
            .SyntaxExpectSymbol("method", new ExpressionValueTypeInfo<AtomExpression>());

    [Theory]
    [InlineData("name gt 'm'")]              // strings are ordered, as documented
    [InlineData("name lte 'm'")]
    [InlineData("age lt 30.5")]              // integers and floats mix
    [InlineData("weight gt 30")]
    [InlineData("age eq 30.0")]
    [InlineData("active")]                   // a boolean field is a valid query by itself
    [InlineData("active and age gt 1")]
    [InlineData("not active")]
    [InlineData("active eq true")]
    [InlineData("name eq nil")]
    [InlineData("method eq :get")]
    [InlineData("name reg '^m'")]
    public void Validate_AcceptsValidQueries(string query)
    {
        Schema(query).Evaluator();
    }

    [Theory]
    [InlineData("age reg 'x'", "only valid for strings")]
    [InlineData("unknown eq 1", "not defined in the current schema")]
    [InlineData("method eq :post", "is not expected")]
    [InlineData("not age", "requires a boolean operand")]
    [InlineData("age and active", "requires boolean operands")]
    [InlineData("age eq 'x'", "Cannot compare")]
    [InlineData("method gt :get", "requires numeric or string operands")]
    [InlineData("42", "must evaluate to a boolean")]
    public void Validate_RejectsInvalidQueries(string query, string expectedMessage)
    {
        var ex = Assert.Throws<QuangTypeException>(() => Schema(query).Evaluator());

        Assert.Contains(expectedMessage, ex.Message);
    }

    [Fact]
    public void Evaluate_MixedNumericComparison_Works()
    {
        var evaluator = Schema("age lt 30.5").Evaluator();

        evaluator.AddIntegerVar("age", 30);
        Assert.True(evaluator.Evaluate());

        evaluator.AddIntegerVar("age", 31);
        Assert.False(evaluator.Evaluate());
    }

    [Fact]
    public void Evaluate_BooleanSymbol_Works()
    {
        var evaluator = Schema("active and age gt 18").Evaluator();

        evaluator.AddBoolVar("active", true).AddIntegerVar("age", 20);
        Assert.True(evaluator.Evaluate());

        evaluator.AddBoolVar("active", false);
        Assert.False(evaluator.Evaluate());
    }

    [Fact]
    public void Evaluate_BooleanComparison_Works()
    {
        var evaluator = Schema("active eq true").Evaluator();

        evaluator.AddBoolVar("active", true);
        Assert.True(evaluator.Evaluate());

        evaluator.AddBoolVar("active", false);
        Assert.False(evaluator.Evaluate());
    }

    [Fact]
    public void Evaluate_StringOrdering_Works()
    {
        var evaluator = Schema("name gt 'm'").Evaluator();

        evaluator.AddStringVar("name", "zoe");
        Assert.True(evaluator.Evaluate());

        evaluator.AddStringVar("name", "alice");
        Assert.False(evaluator.Evaluate());
    }

    [Fact]
    public void SyntaxExpectSymbol_DuplicatedName_Throws()
    {
        var quang = new Quang("age eq 1").Init().SyntaxExpectSymbol("age", new ExpressionValueTypeInfo<IntegerExpression>());

        var ex = Assert.Throws<QuangException>(() =>
            quang.SyntaxExpectSymbol("age", new ExpressionValueTypeInfo<StringExpression>()));

        Assert.Contains("already declared", ex.Message);
    }

    [Fact]
    public void SyntaxExpectAtom_InvalidAtom_Throws()
    {
        var quang = new Quang("method eq :get").Init();

        Assert.Throws<QuangException>(() => quang.SyntaxExpectAtom("get"));
    }

    [Fact]
    public void Evaluator_WithoutInit_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new Quang("true").Evaluator());
    }

    [Fact]
    public void Evaluator_EmptyQuery_ReturnsTrue()
    {
        Assert.True(new Quang("").Init().Evaluator().Evaluate());
    }

    [Fact]
    public void Evaluator_RevalidatesAfterANewSymbolIsDeclared()
    {
        var quang = new Quang("age eq 1").Init();

        Assert.Throws<QuangTypeException>(() => quang.Evaluator());

        quang.SyntaxExpectSymbol("age", new ExpressionValueTypeInfo<IntegerExpression>());

        var evaluator = quang.Evaluator();

        evaluator.AddIntegerVar("age", 1);

        Assert.True(evaluator.Evaluate());
    }
}
