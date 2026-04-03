using System.Text.RegularExpressions;

namespace Quang;

internal class Evaluator
{
    private readonly Dictionary<string, Variable> _symbols;
    private readonly Dictionary<string, Atom> _atoms;
    private readonly Expression? _expression;

    internal Evaluator(Expression? expression)
    {
        _expression = expression;
        _symbols = [];
        _atoms = [];
    }

    internal bool Evaluate() => EvaluateExpression(_expression);

    internal void AddStringVar(string name, string value)
    {
        _symbols[name] = new StringVariable(value);
    }

    internal void AddIntegerVar(string name, int value)
    {
        _symbols[name] = new IntegerVariable(value);
    }

    internal void AddFloatVar(string name, float value)
    {
        _symbols[name] = new FloatVariable(value);
    }

    internal void AddBoolVar(string name, bool value)
    {
        _symbols[name] = new BoolVariable(value);
    }

    internal void AddAtomVar(string name, Atom value)
    {
        _symbols[name] = new AtomVariable(value);
    }

    internal void SetAtomValue(string name, Atom value)
    {
        var tokens = new Lexer(name).Lex();

        if (tokens.Count != 1 || tokens[0].Kind != TokenKind.Atom)
            throw new QuangSyntaxException("invalid atom name");

        _atoms[name] = value;
    }

    private Expression? LazyEvalVar(Expression? expr)
    {
        if (expr is null) return null;

        switch (expr)
        {
            case SymbolExpression symbol:
                if (_symbols.TryGetValue(symbol.Value, out var variable))
                {
                    return variable switch
                    {
                        BoolVariable boolVar => new BoolExpression(boolVar.Value),
                        FloatVariable floatVar => new FloatExpression(floatVar.Value),
                        IntegerVariable intVar => new IntegerExpression(intVar.Integer),
                        AtomVariable atomVar => new AtomExpression(atomVar.Atom),
                        StringVariable stringVar => new StringExpression(stringVar.String),
                        _ => throw new QuangSyntaxException($"could not lazy evaluate type {variable}"),
                    };
                }
                else
                {
                    throw new QuangSyntaxException($"the variable '{symbol.Value}' does not exist");
                }
            case AtomExpression atom:
                if (_atoms.TryGetValue(atom.Value.ToString(), out var atomValue)) return new AtomExpression(atomValue);
                else throw new QuangSyntaxException($"the atom '{atom.Value}' does not exist");
            default:
                break;
        }

        return expr;
    }

    private static bool BinaryComparison(int left, BinaryOperator op, int right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            BinaryOperator.Gt => left > right,
            BinaryOperator.Lt => left < right,
            BinaryOperator.Gte => left >= right,
            BinaryOperator.Lte => left <= right,
            _ => throw new QuangSyntaxException($"you cannot do such operation 'integer {op.ToSymbol()} integer'"),
        };
    }

    private static bool BinaryComparison(float left, BinaryOperator op, float right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            BinaryOperator.Gt => left > right,
            BinaryOperator.Lt => left < right,
            BinaryOperator.Gte => left >= right,
            BinaryOperator.Lte => left <= right,
            _ => throw new QuangSyntaxException($"you cannot do such operation 'float {op.ToSymbol()} float'"),
        };
    }

    private static bool BinaryComparison(string left, BinaryOperator op, string right)
    {
        return op switch
        {
            BinaryOperator.Eq => left == right,
            BinaryOperator.Ne => left != right,
            BinaryOperator.Gt => string.CompareOrdinal(left, right) > 0,
            BinaryOperator.Lt => string.CompareOrdinal(left, right) < 0,
            BinaryOperator.Gte => string.CompareOrdinal(left, right) >= 0,
            BinaryOperator.Lte => string.CompareOrdinal(left, right) <= 0,
            BinaryOperator.Reg => Regex.IsMatch(left, right),
            _ => throw new QuangSyntaxException($"you cannot do such operation 'string {op.ToSymbol()} string'"),
        };
    }

    private static bool CompareAtoms(Atom left, BinaryOperator op, Atom right)
    {
        return op switch
        {
            BinaryOperator.Eq => (string)left == (string)right,
            BinaryOperator.Ne => (string)left != (string)right,
            _ => throw new QuangSyntaxException($"you cannot do such operation 'atom {op.ToSymbol()} atom'"),
        };
    }

    private bool EvaluateBinaryExpression(BinaryExpression binary)
    {
        var op = binary.Operator;

        var left = LazyEvalVar(binary.Left);
        var right = LazyEvalVar(binary.Right);

        switch (op)
        {
            case BinaryOperator.Eq:
            case BinaryOperator.Ne:
            case BinaryOperator.Gt:
            case BinaryOperator.Lt:
            case BinaryOperator.Gte:
            case BinaryOperator.Lte:
            case BinaryOperator.Reg:
                if (left is IntegerExpression a && right is IntegerExpression b)
                    return BinaryComparison(a.Value, op, b.Value);
                else if (left is FloatExpression c && right is FloatExpression d)
                    return BinaryComparison(c.Value, op, d.Value);
                else if (left is StringExpression e && right is StringExpression f)
                    return BinaryComparison(e.Value, op, f.Value);
                else if (left is AtomExpression g && right is AtomExpression h)
                    return CompareAtoms(g.Value, op, h.Value);
                else
                    throw new QuangSyntaxException($"you cannot do such operation '{left?.DisplayName} {op.ToSymbol()} {right?.DisplayName}'");
            case BinaryOperator.Or:
                {
                    var leftValue = EvaluateExpression(binary.Left);
                    var rightValue = EvaluateExpression(binary.Right);

                    return leftValue || rightValue;
                }
            case BinaryOperator.And:
                {
                    var leftValue = EvaluateExpression(binary.Left);
                    var rightValue = EvaluateExpression(binary.Right);

                    return leftValue && rightValue;
                }
        }

        return false;
    }

    private bool EvaluateExpression(Expression? expr)
    {
        return expr switch
        {
            BinaryExpression binary => EvaluateBinaryExpression(binary),
            BoolExpression boolExpr => boolExpr.Value,
            _ => throw new QuangSyntaxException($"could not parse expression kind {expr?.DisplayName}"),
        };
    }
}

internal abstract record Variable;

internal record BoolVariable(bool Value) : Variable;
internal record FloatVariable(float Value) : Variable;
internal record IntegerVariable(int Integer) : Variable;
internal record AtomVariable(Atom Atom) : Variable;
internal record StringVariable(string String) : Variable;