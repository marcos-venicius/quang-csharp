using System.Text.RegularExpressions;

namespace Quang;

public sealed class Evaluator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, Variable> _symbols;
    private readonly HashSet<string> _atoms;
    private readonly Dictionary<string, Regex> _regexes;
    private readonly Expression? _expression;

    internal Evaluator(Expression? expression, HashSet<string> atoms)
    {
        _expression = expression;
        _symbols = [];
        _regexes = [];
        _atoms = atoms;
    }

    /// <summary>
    /// Evaluates the query against the variables added so far.
    /// An empty query always evaluates to true.
    /// </summary>
    public bool Evaluate() => _expression is null || EvaluateExpression(_expression);

    /// <summary>
    /// For each evaluation, you can provide different variable values.
    /// If, for example you want to do a query over a bunch of logs the user
    /// will provide the query, for example filtering by a specific user agent pattern
    /// then, for each log row, you can update the "agent" variable value to the current log row user agent
    /// so the, when the language lazy evaluate the "agent" variable the query will be applied to the current
    /// log row successfully
    /// </summary>
    /// <remarks>A null value is treated as nil.</remarks>
    public Evaluator AddStringVar(string name, string? value)
    {
        _symbols[name] = value is null ? new NilVariable() : new StringVariable(value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddIntegerVar(string name, long value)
    {
        _symbols[name] = new IntegerVariable(value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddIntegerVar(string name, long? value)
    {
        _symbols[name] = value is null ? new NilVariable() : new IntegerVariable(value.Value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddFloatVar(string name, double value)
    {
        _symbols[name] = new FloatVariable(value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddFloatVar(string name, double? value)
    {
        _symbols[name] = value is null ? new NilVariable() : new FloatVariable(value.Value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddBoolVar(string name, bool value)
    {
        _symbols[name] = new BoolVariable(value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddBoolVar(string name, bool? value)
    {
        _symbols[name] = value is null ? new NilVariable() : new BoolVariable(value.Value);

        return this;
    }

    /// <inheritdoc cref="AddStringVar"/>
    public Evaluator AddAtomVar(string name, Atom value)
    {
        _symbols[name] = new AtomVariable(value);

        return this;
    }

    /// <summary>
    /// Declares a variable as empty, so "name eq nil" evaluates to true.
    /// </summary>
    public Evaluator AddNilVar(string name)
    {
        _symbols[name] = new NilVariable();

        return this;
    }

    private Expression LazyEvalVar(Expression expr)
    {
        switch (expr)
        {
            case SymbolExpression symbol:
                if (_symbols.TryGetValue(symbol.Value, out var variable))
                {
                    return variable switch
                    {
                        NilVariable => new NilExpression(),
                        BoolVariable boolVar => new BoolExpression(boolVar.Value),
                        FloatVariable floatVar => new FloatExpression(floatVar.Value),
                        IntegerVariable intVar => new IntegerExpression(intVar.Integer),
                        AtomVariable atomVar => new AtomExpression(atomVar.Atom),
                        StringVariable stringVar => new StringExpression(stringVar.String),
                        _ => throw new QuangEvaluationException($"could not lazy evaluate type {variable}"),
                    };
                }
                else
                {
                    throw new QuangEvaluationException($"the variable '{symbol.Value}' does not exist");
                }
            case AtomExpression atom:
                if (_atoms.TryGetValue(atom.Value.Value, out var atomValue)) return new AtomExpression(new Atom(atomValue));
                else throw new QuangEvaluationException($"the atom '{atom.Value}' does not exist");
            default:
                return expr;
        }
    }

    private static bool BinaryComparison(long left, BinaryOperator op, long right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            BinaryOperator.Gt => left > right,
            BinaryOperator.Lt => left < right,
            BinaryOperator.Gte => left >= right,
            BinaryOperator.Lte => left <= right,
            _ => throw new QuangEvaluationException($"you cannot do such operation 'integer {op.ToSymbol()} integer'"),
        };
    }

    private static bool BinaryComparison(double left, BinaryOperator op, double right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            BinaryOperator.Gt => left > right,
            BinaryOperator.Lt => left < right,
            BinaryOperator.Gte => left >= right,
            BinaryOperator.Lte => left <= right,
            _ => throw new QuangEvaluationException($"you cannot do such operation 'float {op.ToSymbol()} float'"),
        };
    }

    private bool BinaryComparison(string left, BinaryOperator op, string right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            BinaryOperator.Gt => string.CompareOrdinal(left, right) > 0,
            BinaryOperator.Lt => string.CompareOrdinal(left, right) < 0,
            BinaryOperator.Gte => string.CompareOrdinal(left, right) >= 0,
            BinaryOperator.Lte => string.CompareOrdinal(left, right) <= 0,
            BinaryOperator.Reg => IsMatch(left, right),
            _ => throw new QuangEvaluationException($"you cannot do such operation 'string {op.ToSymbol()} string'"),
        };
    }

    private static bool CompareAtoms(Atom left, BinaryOperator op, Atom right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            _ => throw new QuangEvaluationException($"you cannot do such operation 'atom {op.ToSymbol()} atom'"),
        };
    }

    private static bool CompareBools(bool left, BinaryOperator op, bool right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            _ => throw new QuangEvaluationException($"you cannot do such operation 'bool {op.ToSymbol()} bool'"),
        };
    }

    /// <summary>
    /// nil represents every kind of empty value, so an empty string is nil as well.
    /// Zero, false and empty atoms are not considered empty.
    /// </summary>
    private static bool IsEmptyValue(Expression expr) => expr switch
    {
        NilExpression => true,
        StringExpression str => str.Value.Length == 0,
        _ => false,
    };

    private bool IsMatch(string input, string pattern)
    {
        if (!_regexes.TryGetValue(pattern, out var regex))
        {
            try
            {
                regex = new Regex(pattern, RegexOptions.None, RegexTimeout);
            }
            catch (ArgumentException ex)
            {
                throw new QuangEvaluationException($"invalid regex pattern '{pattern}': {ex.Message}");
            }

            _regexes[pattern] = regex;
        }

        try
        {
            return regex.IsMatch(input);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new QuangEvaluationException($"the regex pattern '{pattern}' took too long to run");
        }
    }

    private bool EvaluateBinaryExpression(BinaryExpression binary)
    {
        var op = binary.Operator;

        // logical operators short circuit, so the right side only runs when it is needed
        if (op == BinaryOperator.And)
            return EvaluateExpression(binary.Left) && EvaluateExpression(binary.Right);

        if (op == BinaryOperator.Or)
            return EvaluateExpression(binary.Left) || EvaluateExpression(binary.Right);

        var left = LazyEvalVar(binary.Left);
        var right = LazyEvalVar(binary.Right);

        if (left is NilExpression || right is NilExpression)
        {
            var equal = IsEmptyValue(left) && IsEmptyValue(right);

            return op switch
            {
                BinaryOperator.Eq => equal,
                BinaryOperator.Ne => !equal,
                _ => throw new QuangEvaluationException($"you cannot do such operation '{left.DisplayName} {op.ToSymbol()} {right.DisplayName}'"),
            };
        }

        if (left is IntegerExpression a && right is IntegerExpression b)
            return BinaryComparison(a.Value, op, b.Value);

        if (left is FloatExpression c && right is FloatExpression d)
            return BinaryComparison(c.Value, op, d.Value);

        // integers and floats are compared as floats
        if (left is IntegerExpression ai && right is FloatExpression bf)
            return BinaryComparison(ai.Value, op, bf.Value);

        if (left is FloatExpression af && right is IntegerExpression bi)
            return BinaryComparison(af.Value, op, bi.Value);

        if (left is StringExpression e && right is StringExpression f)
            return BinaryComparison(e.Value, op, f.Value);

        if (left is AtomExpression g && right is AtomExpression h)
            return CompareAtoms(g.Value, op, h.Value);

        if (left is BoolExpression i && right is BoolExpression j)
            return CompareBools(i.Value, op, j.Value);

        throw new QuangEvaluationException($"you cannot do such operation '{left.DisplayName} {op.ToSymbol()} {right.DisplayName}'");
    }

    private bool EvaluateUnaryExpression(UnaryExpression unary)
    {
        var op = unary.Operator;

        return op switch
        {
            UnaryOperator.Not => !EvaluateExpression(unary.Expr),
            _ => throw new QuangEvaluationException($"could not evaluate {op.ToSymbol()} operator"),
        };
    }

    private bool EvaluateExpression(Expression expr)
    {
        // a variable can be used directly as a boolean, like in "active and status eq 200"
        if (expr is SymbolExpression symbol)
        {
            var value = LazyEvalVar(symbol);

            if (value is BoolExpression boolValue) return boolValue.Value;

            throw new QuangEvaluationException($"the variable '{symbol.Value}' is not a boolean, it is a {value.DisplayName}");
        }

        return expr switch
        {
            BinaryExpression binary => EvaluateBinaryExpression(binary),
            UnaryExpression unary => EvaluateUnaryExpression(unary),
            BoolExpression boolExpr => boolExpr.Value,
            _ => throw new QuangEvaluationException($"could not parse expression kind {expr.DisplayName}"),
        };
    }
}

internal abstract record Variable;

internal record NilVariable : Variable;
internal record BoolVariable(bool Value) : Variable;
internal record FloatVariable(double Value) : Variable;
internal record IntegerVariable(long Integer) : Variable;
internal record AtomVariable(Atom Atom) : Variable;
internal record StringVariable(string String) : Variable;
