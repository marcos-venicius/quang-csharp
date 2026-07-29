namespace Quang.Tests;

/// <summary>
/// Every query used as an example in LLM.md is checked here, so the documentation
/// cannot drift away from the language.
/// </summary>
public class DocumentationTests
{
    // the schema documented in LLM.md
    private static Quang Schema(string query) =>
        new Quang(query)
            .Init()
            .SyntaxExpectAtom(":get")
            .SyntaxExpectAtom(":post")
            .SyntaxExpectAtom(":put")
            .SyntaxExpectAtom(":delete")
            .SyntaxExpectSymbol("status", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("size", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("latency", new ExpressionValueTypeInfo<FloatExpression>())
            .SyntaxExpectSymbol("path", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("agent", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("method", new ExpressionValueTypeInfo<AtomExpression>())
            .SyntaxExpectSymbol("cached", new ExpressionValueTypeInfo<BoolExpression>());

    [Theory]
    [InlineData("")]
    [InlineData("status gte 400")]
    [InlineData("method eq :get and status gte 200 and status lt 300")]
    [InlineData("not (status gte 300 and status lt 400)")]
    [InlineData("method eq :get or method eq :post")]
    [InlineData("latency gt 1.5")]
    [InlineData(@"path reg '^/api/'")]
    [InlineData(@"path reg '^/users/\d+$'")]
    [InlineData(@"agent reg '(?i)curl'")]
    [InlineData("agent eq nil")]
    [InlineData("agent ne nil")]
    [InlineData("cached")]
    [InlineData("not cached")]
    [InlineData("size gt 1000000 and latency gt 2.0")]
    [InlineData(@"(path reg '^/api/' and status gte 500) or latency gt 10.0")]
    [InlineData("status gte 200 and status lte 299")]
    [InlineData("not status eq 400 and cached or size gt 10")]
    [InlineData("(status gte 400) eq true")]
    [InlineData("agent eq ''")]
    [InlineData("cached eq nil")]
    [InlineData("latency gt 1")]
    [InlineData("status lt 30.5")]
    public void DocumentedQueries_AreValid(string query)
    {
        Schema(query).Evaluator();
    }

    [Theory]
    [InlineData("status = 200")]
    [InlineData("status != 200")]
    [InlineData("status > 400")]
    [InlineData("status gte 400 && cached")]
    [InlineData("NOT (status eq 400)")]
    [InlineData("status in (200, 201)")]
    [InlineData("status between 200 and 299")]
    [InlineData("path like '%api%'")]
    [InlineData("path eq \"/api\"")]
    [InlineData("size gt -5")]
    [InlineData("latency gt .5")]
    [InlineData("size gt 1e6")]
    [InlineData("method eq 'get'")]
    [InlineData("method eq :patch")]
    [InlineData("status reg '^4'")]
    [InlineData("status eq 200 path eq '/'")]
    [InlineData("status gt 18 eq true")]
    [InlineData("42")]
    [InlineData("not status")]
    public void DocumentedMistakes_AreRejected(string query)
    {
        Assert.ThrowsAny<QuangException>(() => Schema(query).Evaluator());
    }

    [Fact]
    public void DocumentedSemantics_BehaveAsDescribed()
    {
        var evaluator = Schema("status gte 400 or (agent eq nil and not cached)").Evaluator();

        evaluator
            .AddIntegerVar("status", 200)
            .AddIntegerVar("size", 10)
            .AddFloatVar("latency", 0.1)
            .AddStringVar("path", "/api/users")
            .AddStringVar("agent", null)
            .AddAtomVar("method", ":get")
            .AddBoolVar("cached", false);

        Assert.True(evaluator.Evaluate());

        evaluator.AddBoolVar("cached", true);
        Assert.False(evaluator.Evaluate());

        evaluator.AddIntegerVar("status", 500);
        Assert.True(evaluator.Evaluate());

        // an empty string is nil, zero is not
        var empty = Schema("agent eq nil").Evaluator();

        empty.AddStringVar("agent", "");
        Assert.True(empty.Evaluate());

        var zero = Schema("status eq nil").Evaluator();

        zero.AddIntegerVar("status", 0);
        Assert.False(zero.Evaluate());

        // ordinal string comparison: uppercase sorts before lowercase
        var ordinal = Schema("path gt 'Z'").Evaluator();

        ordinal.AddStringVar("path", "a");
        Assert.True(ordinal.Evaluate());

        // reg is an unanchored search with .NET syntax
        var search = Schema(@"path reg '^/users/\d+$'").Evaluator();

        search.AddStringVar("path", "/users/42");
        Assert.True(search.Evaluate());

        search.AddStringVar("path", "/users/marcos");
        Assert.False(search.Evaluate());

        // an empty query matches everything
        Assert.True(Schema("").Evaluator().Evaluate());
    }

    // the shape documented in section 12.2 of LLM.md
    public enum HttpMethod
    {
        GET,
        POST
    }

    public class LogRow
    {
        public int Status { get; set; }
        public double Latency { get; set; }
        public string? Path { get; set; }
        public HttpMethod Method { get; set; }
        public bool Cached { get; set; }
    }

    private static Quang LogSchema(string query) =>
        new Quang(query)
            .Init()
            .SyntaxExpectAtom(":get")
            .SyntaxExpectAtom(":post")
            .SyntaxExpectSymbol("status", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("latency", new ExpressionValueTypeInfo<FloatExpression>())
            .SyntaxExpectSymbol("path", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("method", new ExpressionValueTypeInfo<AtomExpression>())
            .SyntaxExpectSymbol("cached", new ExpressionValueTypeInfo<BoolExpression>());

    [Fact]
    public void DocumentedTranslator_WorksAsDescribed()
    {
        var rows = new List<LogRow>
        {
            new() { Status = 500, Latency = 0.2, Path = "/api/users", Method = HttpMethod.GET, Cached = false },
            new() { Status = 200, Latency = 3.0, Path = "/home", Method = HttpMethod.POST, Cached = true },
            new() { Status = 200, Latency = 0.1, Path = null, Method = HttpMethod.GET, Cached = true },
        };

        // default options: atoms match the enum member, "reg" is a real regex
        var predicate = new Interpreters.LinqInterpreter<LogRow>()
            .Translate(LogSchema(@"method eq :get and (status gte 500 or path reg '^/api/')"))
            .Compile();

        Assert.Equal(1, rows.Count(predicate));

        // documented mappings and the Contains strategy
        var mapped = new Interpreters.LinqInterpreter<LogRow>(
                symbolsMapping: new() { { "codigo", "Status" } },
                atomsMapping: new() { { ":get", "GET" } },
                regStrategy: Interpreters.RegStrategy.Contains)
            .Translate(LogSchema("codigo eq 200 and path reg 'home'")
                .SyntaxExpectSymbol("codigo", new ExpressionValueTypeInfo<IntegerExpression>()))
            .Compile();

        Assert.Equal(1, rows.Count(mapped));

        // boolean field, flipped operands, nil and numeric coercion
        Assert.Equal(2, rows.Count(Translate("cached")));
        Assert.Equal(1, rows.Count(Translate("500 lte status")));
        Assert.Equal(1, rows.Count(Translate("path eq nil")));
        Assert.Equal(2, rows.Count(Translate("path ne nil")));
        Assert.Equal(1, rows.Count(Translate("latency gt 1")));
        Assert.Equal(3, rows.Count(Translate("(status gte 500 or cached) eq true")));

        static Func<LogRow, bool> Translate(string query) =>
            new Interpreters.LinqInterpreter<LogRow>().Translate(LogSchema(query)).Compile();
    }

    [Theory]
    [InlineData("status reg 'x'", "only valid for strings")]
    [InlineData("200 eq 200", "one side of a comparison must be a field name")]
    public void DocumentedTranslatorLimits_AreRejected(string query, string expectedMessage)
    {
        var ex = Assert.ThrowsAny<QuangException>(() =>
            new Interpreters.LinqInterpreter<LogRow>().Translate(LogSchema(query)));

        Assert.Contains(expectedMessage, ex.Message);
    }

    [Fact]
    public void DocumentedTranslator_ComparesFieldsAndOrdersStrings()
    {
        var rows = new List<LogRow>
        {
            new() { Status = 500, Latency = 0.2, Path = "/api/users" },
            new() { Status = 200, Latency = 3.0, Path = "/home" },
            new() { Status = 200, Latency = 0.1, Path = null },
        };

        // field against field, with an int promoted to a double
        Assert.Equal(3, rows.Count(Translate("status gt latency")));
        Assert.Equal(0, rows.Count(Translate("latency gte status")));

        // ordinal string ordering, like the evaluator
        Assert.Equal(1, rows.Count(Translate("path gt '/b'")));

        // a literal can be the input of reg, and a field can be the pattern
        Assert.Equal(1, rows.Count(Translate("'/home/nested' reg path")));

        static Func<LogRow, bool> Translate(string query) =>
            new Interpreters.LinqInterpreter<LogRow>().Translate(LogSchema(query)).Compile();
    }

    [Fact]
    public void DocumentedPrecedence_IsRespected()
    {
        // not status eq 400 and cached or size gt 10
        //   ==  (((not (status eq 400)) and cached) or (size gt 10))
        var evaluator = Schema("not status eq 400 and cached or size gt 10").Evaluator();

        evaluator.AddIntegerVar("status", 400).AddBoolVar("cached", true).AddIntegerVar("size", 5);
        Assert.False(evaluator.Evaluate());

        evaluator.AddIntegerVar("status", 200);
        Assert.True(evaluator.Evaluate());

        evaluator.AddBoolVar("cached", false).AddIntegerVar("size", 20);
        Assert.True(evaluator.Evaluate());
    }
}
