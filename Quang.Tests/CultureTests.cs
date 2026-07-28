using System.Globalization;

namespace Quang.Tests;

/// <summary>
/// Number literals must always be read with the invariant culture.
/// On a culture where "." is the group separator, "70.2" used to be parsed as 702.
/// </summary>
public class CultureTests
{
    private static CultureInfo CommaDecimalCulture()
    {
        var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();

        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";

        return culture;
    }

    private static bool Evaluate(string query)
    {
        var expr = new Parser(new Lexer(query).Lex()).Parse();

        return new Evaluator(expr, []).Evaluate();
    }

    [Theory]
    [InlineData("70.2 lt 100", true)]
    [InlineData("10.5 lt 20", true)]
    [InlineData("1.5 eq 1.5", true)]
    [InlineData("1.5 gt 1.4", true)]
    [InlineData("0.5 lt 1", true)]
    public void Evaluate_FloatLiterals_AreCultureIndependent(string query, bool expected)
    {
        var previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CommaDecimalCulture();

            Assert.Equal(expected, Evaluate(query));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Evaluate_FloatVariables_AreCultureIndependent()
    {
        var previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CommaDecimalCulture();

            var expr = new Parser(new Lexer("weight lte 70.0").Lex()).Parse();
            var evaluator = new Evaluator(expr, []);

            evaluator.AddFloatVar("weight", 69.9);
            Assert.True(evaluator.Evaluate());

            evaluator.AddFloatVar("weight", 70.1);
            Assert.False(evaluator.Evaluate());
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
