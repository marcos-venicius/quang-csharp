namespace Quang.Tests;

public class QuangTests
{
    private record TestCase(int Size, Atom Method, int Status, bool Result);

    [Fact]
    public void Evaluate_ApiIntegration_ReturnsExpectedResults()
    {
        var q = new Quang("size gt 0 and method eq :get and status eq 200").Init();

        var atoms = new Dictionary<string, Atom>
        {
            { ":get", "get" }
        };

        q.SetupAtoms(atoms);

        List<TestCase> tests =
        [
            new(0, atoms[":get"], 200, false ),
            new(1, atoms[":get"], 200, true ),
            new(1, "post", 200, false ),
            new(1, atoms[":get"], 204, false ),
        ];

        foreach (var test in tests)
        {
            q.AddAtomVar("method", test.Method)
             .AddIntegerVar("size", test.Size)
             .AddIntegerVar("status", test.Status);

            var r = q.Evaluate();

            Assert.Equal(test.Result, r);
        }
    }
}