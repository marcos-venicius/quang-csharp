using System.Linq.Expressions;

using LinqExpression = System.Linq.Expressions.Expression;

namespace Quang.Interpreters;

public sealed class LinqInterpreter<T>
{
    private readonly Dictionary<string, string> _symbolsMapping = [];
    private readonly Dictionary<string, string> _atomsMapping = [];

    public LinqInterpreter(Dictionary<string, string>? symbolsMapping = null, Dictionary<string, string>? atomsMapping = null)
    {
        if (symbolsMapping is not null) _symbolsMapping = symbolsMapping;
        if (atomsMapping is not null) _atomsMapping = atomsMapping;
    }

    private static readonly ParameterExpression Param = LinqExpression.Parameter(typeof(T), "x");

    public Expression<Func<T, bool>> Translate(Quang quang)
    {
        var root = quang.GetExpressionTree();

        if (root == null)
            return LinqExpression.Lambda<Func<T, bool>>(LinqExpression.Constant(true), Param);

        var body = Visit(root);
        return LinqExpression.Lambda<Func<T, bool>>(body, Param);
    }

    private LinqExpression Visit(Expression expr)
    {
        return expr switch
        {
            BinaryExpression binary => VisitBinary(binary),
            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} is not supported as a root/logical node.")
        };
    }

    private LinqExpression VisitBinary(BinaryExpression binary)
    {
        // Handle logical combinators (And, Or)
        if (binary.Operator == BinaryOperator.And)
            return LinqExpression.AndAlso(Visit(binary.Left), Visit(binary.Right));
        if (binary.Operator == BinaryOperator.Or)
            return LinqExpression.OrElse(Visit(binary.Left), Visit(binary.Right));

        // Handle Comparisons (Eq, Ne, etc.)
        // Left side is usually the property (Symbol), Right side is the constant
        var left = GetMemberExpression(binary.Left);
        var right = GetConstantExpression(binary.Right);

        return binary.Operator switch
        {
            BinaryOperator.Eq => LinqExpression.Equal(left, right),
            BinaryOperator.Ne => LinqExpression.NotEqual(left, right),
            BinaryOperator.Gt => LinqExpression.GreaterThan(left, right),
            BinaryOperator.Lt => LinqExpression.LessThan(left, right),
            BinaryOperator.Gte => LinqExpression.GreaterThanOrEqual(left, right),
            BinaryOperator.Lte => LinqExpression.LessThanOrEqual(left, right),
            BinaryOperator.Reg => BuildRegexExpression(left, right),
            _ => throw new NotImplementedException()
        };
    }

    private MemberExpression GetMemberExpression(Expression expr)
    {
        if (expr is SymbolExpression symbol)
        {
            var propertyName = MapSymbol(symbol.Value);

            return LinqExpression.Property(Param, propertyName);
        }
        throw new Exception("Left side of comparison must be a field name.");
    }

    private string MapSymbol(string name)
    {
        if (_symbolsMapping.TryGetValue(name, out var translation))
            return translation;

        return name;
    }

    private string MapAtom(string name)
    {
        if (_atomsMapping.TryGetValue(name, out var translation))
            return translation;

        return name;
    }

    private ConstantExpression GetConstantExpression(Expression expr)
    {
        return expr switch
        {
            NilExpression => LinqExpression.Constant(null),
            IntegerExpression i => LinqExpression.Constant(i.Value),
            FloatExpression f => LinqExpression.Constant(f.Value),
            BoolExpression b => LinqExpression.Constant(b.Value),
            StringExpression s => LinqExpression.Constant(s.Value),
            AtomExpression a => LinqExpression.Constant(MapAtom((string)a.Value)),
            _ => throw new Exception($"Unsupported constant type: {expr.GetType().Name}")
        };
    }

    private LinqExpression BuildRegexExpression(LinqExpression left, ConstantExpression right)
    {
        var pattern = right;

        if (right.Type != typeof(string))
            pattern = LinqExpression.Constant(right.Value?.ToString());

        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);

        return LinqExpression.Call(left, containsMethod!, pattern);
    }
}