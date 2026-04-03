namespace Quang.Tests;

public class EvaluatorTests
{
    [Fact]
    public void Evaluate_BooleanExpressions_ReturnsExpectedResults()
    {
        var tests = new Dictionary<string, bool>
        {
            { "true or false", true },
            { "false or true", true },
            { "true or true", true },
            { "false or false", false },
            { "true and false", false },
            { "false and true", false },
            { "false and false", false },
            { "true and true", true },
            { "true", true },
            { "false", false },
            { "(true)", true },
            { "(true) or (true)", true },
            { "(true) or true", true },
            { "true or (true)", true },
            { "(false) or (false)", false },
            { "(true) and (true)", true },
            { "(false) and (false)", false },
            { "(false and true) or ((true and false) or true)", true },
            { "true and false or true", true },
        };

        foreach (var (input, expected) in tests)
        {
            var tokens = new Lexer(input).Lex();
            var parser = new Parser(tokens);
            var expr = parser.ParseExpression();

            Assert.NotNull(expr);

            var evaluator = new Evaluator(expr!);
            var result = evaluator.Evaluate();

            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void Evaluate_IntegerExpressions_ReturnsExpectedResults()
    {
        var tests = new Dictionary<string, bool>
        {
            { "1 eq 1", true },
            { "1 eq 10", false },
            { "2 eq 1", false },
            { "(1 eq 1)", true },
            { "((1 eq 1))", true },
            { "1 ne 1", false },
            { "1 ne 2", true },
            { "2 ne 1", true },
            { "10 gt 5", true },
            { "10 gt 15", false },
            { "15 gt 10", true },
            { "10 gt 10", false },

            { "10 lt 5", false },
            { "10 lt 15", true },
            { "15 lt 10", false },
            { "10 lt 10", false },

            { "10 gte 10", true },
            { "10 gte 11", false },
            { "11 gte 10", true },

            { "10 lte 10", true },
            { "10 lte 11", true },
            { "11 lte 10", false },

            { "(true and false) or (1 gte 0 or 10 lte 5)", true },
        };

        foreach (var (input, expected) in tests)
        {
            var tokens = new Lexer(input).Lex();
            var parser = new Parser(tokens);
            var expr = parser.ParseExpression();

            Assert.NotNull(expr);

            var evaluator = new Evaluator(expr!);
            var result = evaluator.Evaluate();

            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void Evaluate_FloatExpressions_ReturnsExpectedResults_AndHandlesErrors()
    {
        var tests = new Dictionary<string, bool>
        {
            { "1.5 eq 1.5", true },
            { "1.5 eq 10.309", false },
            { "2. eq 1.9", false },
            { "(1. eq 1.)", true },
            { "((1. eq 1.0))", true },
            { "1. ne 1.0001", true },
            { "1. ne 2.", true },
            { "2. ne 1.", true },
            { "10. gt 5.", true },
            { "10. gt 15.", false },
            { "15. gt 10.", true },
            { "10. gt 10.", false },

            { "10. lt 5.", false },
            { "10. lt 15.", true },
            { "15. lt 10.", false },
            { "10. lt 10.", false },

            { "10. gte 10.", true },
            { "10. gte 11.", false },
            { "11. gte 10.", true },

            { "10. lte 10.", true },
            { "10. lte 11.", true },
            { "(10. lte 11.)", true },
            { "11. lte 10.", false },
        };

        foreach (var (input, expected) in tests)
        {
            var tokens = new Lexer(input).Lex();
            var parser = new Parser(tokens);
            var expr = parser.ParseExpression();

            Assert.NotNull(expr);

            var evaluator = new Evaluator(expr!);
            var result = evaluator.Evaluate();

            Assert.Equal(expected, result);
        }

        var failTests = new[]
        {
            new
            {
                Expr = "10. reg 'dsflksjdf'",
                Error = "error: you cannot do such operation 'float reg string'",
                Result = false
            },
            new
            {
                Expr = "'dsflksjdf' reg 10.",
                Error = "error: you cannot do such operation 'string reg float'",
                Result = false
            }
        };

        foreach (var test in failTests)
        {
            var tokens = new Lexer(test.Expr).Lex();
            var parser = new Parser(tokens);
            var expr = parser.ParseExpression();

            Assert.NotNull(expr);

            var evaluator = new Evaluator(expr!);

            var ex = Assert.Throws<QuangSyntaxException>(() => evaluator.Evaluate());
            Assert.Equal(test.Error, ex.Message);
        }
    }

    [Fact]
    public void Evaluate_StringExpressions_ReturnsExpectedResults()
    {
        var tests = new Dictionary<string, bool>
        {
            { "'hello world' eq 'hello world'", true },
            { "'hello world ' eq 'hello world'", false },
            { "'hello world ' ne 'hello world'", true },
            { "'hello world' ne 'hello world'", false },
            { "'hello \\'world' eq 'hello \\'world'", true },
            { "'z' gt 'a'", true },
            { "'a' lt 'z'", true },
            { "'a' eq 'a'", true },
            {
                "'/test/3e7f0bb3-d315-46ec-a92f-9bd694e5e281/fake' reg '^/test/[a-z0-9]{8}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{4}-[a-z0-9]{12}/fake$'",
                true
            },
        };

        foreach (var (input, expected) in tests)
        {
            var tokens = new Lexer(input).Lex();
            var parser = new Parser(tokens);
            var expr = parser.ParseExpression();

            Assert.NotNull(expr);

            var evaluator = new Evaluator(expr!);
            var result = evaluator.Evaluate();

            Assert.Equal(expected, result);
        }
    }

    [Fact]
    public void Evaluate_LazySymbols_ReturnsExpectedResults()
    {
        var test = "size gt 40";

        var tokens = new Lexer(test).Lex();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression();

        Assert.NotNull(expr);

        var evaluator = new Evaluator(expr!);

        // Variable does not exist
        var ex1 = Assert.Throws<QuangSyntaxException>(() => evaluator.Evaluate());
        Assert.Equal("error: the variable 'size' does not exist", ex1.Message);

        // Add string variable, incompatible operation
        evaluator.AddStringVar("size", "anything");
        var ex2 = Assert.Throws<QuangSyntaxException>(() => evaluator.Evaluate());
        Assert.Equal("error: you cannot do such operation 'string gt integer'", ex2.Message);

        // Add integer variable 41
        evaluator.AddIntegerVar("size", 41);
        var result = evaluator.Evaluate();
        Assert.True(result);

        // Change integer variable to 38
        evaluator.AddIntegerVar("size", 38);
        result = evaluator.Evaluate();
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_LazySymbolsWithStrings_ReturnsExpectedResult()
    {
        var test = "agent reg this";

        var tokens = new Lexer(test).Lex();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression();

        Assert.NotNull(expr);

        var evaluator = new Evaluator(expr!);

        evaluator.AddStringVar("agent", "hello world");
        evaluator.AddStringVar("this", "^\\w+\\s\\w+$");

        var result = evaluator.Evaluate();

        Assert.True(result);
    }

    [Fact]
    public void Evaluate_LazyAtoms_ReturnsExpectedResults()
    {
        var test = "method eq :get";

        var tokens = new Lexer(test).Lex();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression();

        Assert.NotNull(expr);

        var evaluator = new Evaluator(expr!);

        // Variable does not exist
        var ex1 = Assert.Throws<QuangSyntaxException>(() => evaluator.Evaluate());
        Assert.Equal("error: the variable 'method' does not exist", ex1.Message);

        // Add atom variable but target atom missing
        evaluator.AddAtomVar("method", new Atom("get"));
        var ex2 = Assert.Throws<QuangSyntaxException>(() => evaluator.Evaluate());
        Assert.Equal("error: the atom ':get' does not exist", ex2.Message);

        // Set target atom to :post
        evaluator.SetAtomValue(":get", new Atom("post"));
        var result = evaluator.Evaluate();
        Assert.False(result);

        // Set target atom to :get
        evaluator.SetAtomValue(":get", new Atom("get"));
        result = evaluator.Evaluate();
        Assert.True(result);

        // Add string variable, incompatible operation
        evaluator.AddStringVar("method", "get");
        var ex3 = Assert.Throws<QuangSyntaxException>(() => evaluator.Evaluate());
        Assert.Equal("error: you cannot do such operation 'string eq atom'", ex3.Message);
    }

    [Fact]
    public void Evaluate_LazyAtomsNeOperator_ReturnsExpectedResults()
    {
        var test = "method ne :get";

        var tokens = new Lexer(test).Lex();
        var parser = new Parser(tokens);
        var expr = parser.ParseExpression();

        Assert.NotNull(expr);

        var evaluator = new Evaluator(expr!);

        evaluator.AddAtomVar("method", new Atom("get"));
        evaluator.SetAtomValue(":get", new Atom("get"));

        var result = evaluator.Evaluate();
        Assert.False(result);

        evaluator.SetAtomValue(":get", new Atom("post"));
        result = evaluator.Evaluate();
        Assert.True(result);
    }
}