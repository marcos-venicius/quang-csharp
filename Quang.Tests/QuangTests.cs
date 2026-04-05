namespace Quang.Tests;

public class QuangTests
{
    private record TestCase(int Size, Atom Method, int Status, bool Result);

    [Fact]
    public void Evaluate_ApiIntegration_ReturnsExpectedResults()
    {
        var q = new Quang("size gt 0 and method eq :get and status eq 200")
            .Init()
            .SyntaxExpectAtom(":get")
            .SyntaxExpectSymbol("size", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("status", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("method", new ExpressionValueTypeInfo<AtomExpression>());

        List<TestCase> tests =
        [
            new(0, ":get", 200, false ),
            new(1, ":get", 200, true ),
            new(1, ":post", 200, false ),
            new(1, ":get", 204, false ),
        ];

        var evaluator = q.Evaluator();

        foreach (var test in tests)
        {
            evaluator.AddAtomVar("method", test.Method)
             .AddIntegerVar("size", test.Size)
             .AddIntegerVar("status", test.Status);

            var r = evaluator.Evaluate();

            Assert.Equal(test.Result, r);
        }
    }
}