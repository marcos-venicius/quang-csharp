using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

using LinqExpression = System.Linq.Expressions.Expression;

namespace Quang.Interpreters;

/// <summary>
/// How the "reg" operator should be translated.
/// </summary>
public enum RegStrategy
{
    /// <summary>
    /// Translates to Regex.IsMatch, which is exactly what the Evaluator does.
    /// Providers like EF Core cannot translate it to SQL, so it runs in memory.
    /// </summary>
    Regex,

    /// <summary>
    /// Translates to string.Contains (a plain substring match), which EF Core turns into a SQL LIKE.
    /// </summary>
    Contains
}

public sealed class LinqInterpreter<T>
{
    private static readonly MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    private static readonly MethodInfo RegexIsMatchMethod =
        typeof(Regex).GetMethod(nameof(Regex.IsMatch), [typeof(string), typeof(string)])!;

    private static readonly MethodInfo IsNullOrEmptyMethod =
        typeof(string).GetMethod(nameof(string.IsNullOrEmpty), [typeof(string)])!;

    private readonly Dictionary<string, string> _symbolsMapping = [];
    private readonly Dictionary<string, string> _atomsMapping = [];
    private readonly RegStrategy _regStrategy;
    private readonly ParameterExpression _param = LinqExpression.Parameter(typeof(T), "x");

    public LinqInterpreter(
        Dictionary<string, string>? symbolsMapping = null,
        Dictionary<string, string>? atomsMapping = null,
        RegStrategy regStrategy = RegStrategy.Regex)
    {
        if (symbolsMapping is not null) _symbolsMapping = symbolsMapping;
        if (atomsMapping is not null) _atomsMapping = atomsMapping;

        _regStrategy = regStrategy;
    }

    public Expression<Func<T, bool>> Translate(Quang quang)
    {
        var root = quang.GetExpressionTree();

        if (root == null)
            return LinqExpression.Lambda<Func<T, bool>>(LinqExpression.Constant(true), _param);

        var body = Visit(root);

        return LinqExpression.Lambda<Func<T, bool>>(body, _param);
    }

    private LinqExpression Visit(Expression expr)
    {
        return expr switch
        {
            BinaryExpression binary => VisitBinary(binary),
            UnaryExpression unary => VisitUnary(unary),
            BoolExpression boolean => LinqExpression.Constant(boolean.Value),
            SymbolExpression symbol => VisitBooleanSymbol(symbol),
            _ => throw new QuangEvaluationException($"expression {expr.DisplayName} is not supported as a logical node.")
        };
    }

    /// <summary>
    /// Supports using a boolean field directly, like "active and status eq 200".
    /// </summary>
    private LinqExpression VisitBooleanSymbol(SymbolExpression symbol)
    {
        var member = GetMemberExpression(symbol);

        if (member.Type == typeof(bool)) return member;

        if (member.Type == typeof(bool?))
            return LinqExpression.Equal(member, LinqExpression.Constant(true, typeof(bool?)));

        throw new QuangEvaluationException($"the field '{symbol.Value}' is not a boolean, it is a {member.Type.Name}");
    }

    private LinqExpression VisitUnary(UnaryExpression unary)
    {
        return unary.Operator switch
        {
            UnaryOperator.Not => LinqExpression.Not(Visit(unary.Expr)),
            _ => throw new QuangEvaluationException($"unary operator {unary.Operator} is not supported.")
        };
    }

    private LinqExpression VisitBinary(BinaryExpression binary)
    {
        // Handle logical combinators (And, Or)
        if (binary.Operator == BinaryOperator.And)
            return LinqExpression.AndAlso(Visit(binary.Left), Visit(binary.Right));

        if (binary.Operator == BinaryOperator.Or)
            return LinqExpression.OrElse(Visit(binary.Left), Visit(binary.Right));

        return VisitComparison(binary);
    }

    private LinqExpression VisitComparison(BinaryExpression binary)
    {
        var op = binary.Operator;

        // a nested expression can be compared as a boolean, like in "(a or b) eq true"
        if (binary.Left is BinaryExpression or UnaryExpression || binary.Right is BinaryExpression or UnaryExpression)
        {
            if (op is not (BinaryOperator.Eq or BinaryOperator.Ne))
                throw new QuangEvaluationException($"operator '{op.ToSymbol()}' cannot be applied to a boolean expression.");

            var nestedLeft = Visit(binary.Left);
            var nestedRight = Visit(binary.Right);

            return op == BinaryOperator.Eq
                ? LinqExpression.Equal(nestedLeft, nestedRight)
                : LinqExpression.NotEqual(nestedLeft, nestedRight);
        }

        var leftSymbol = binary.Left as SymbolExpression;
        var rightSymbol = binary.Right as SymbolExpression;

        if (leftSymbol is not null && rightSymbol is not null)
            throw new QuangEvaluationException("comparing two fields is not supported by the LINQ interpreter.");

        MemberExpression member;
        Expression value;

        if (leftSymbol is not null)
        {
            member = GetMemberExpression(leftSymbol);
            value = binary.Right;
        }
        else if (rightSymbol is not null)
        {
            // the field is on the right side, so "200 lt status" becomes "status gt 200"
            member = GetMemberExpression(rightSymbol);
            value = binary.Left;
            op = op.Flip();
        }
        else
        {
            throw new QuangEvaluationException("one side of a comparison must be a field name.");
        }

        if (op == BinaryOperator.Reg)
        {
            if (leftSymbol is null)
                throw new QuangEvaluationException("the left side of 'reg' must be a field name.");

            return BuildRegExpression(member, value);
        }

        if (value is NilExpression) return BuildNilComparison(member, op);

        var constant = GetConstantExpression(member, value);

        return op switch
        {
            BinaryOperator.Eq => LinqExpression.Equal(member, constant),
            BinaryOperator.Ne => LinqExpression.NotEqual(member, constant),
            BinaryOperator.Gt => LinqExpression.GreaterThan(member, constant),
            BinaryOperator.Lt => LinqExpression.LessThan(member, constant),
            BinaryOperator.Gte => LinqExpression.GreaterThanOrEqual(member, constant),
            BinaryOperator.Lte => LinqExpression.LessThanOrEqual(member, constant),
            _ => throw new QuangEvaluationException($"operator {op.ToSymbol()} is not supported by the LINQ interpreter.")
        };
    }

    /// <summary>
    /// nil means "empty", so an empty string matches nil as it does in the Evaluator.
    /// A non nullable value type can never be empty.
    /// </summary>
    private static LinqExpression BuildNilComparison(MemberExpression member, BinaryOperator op)
    {
        if (op != BinaryOperator.Eq && op != BinaryOperator.Ne)
            throw new QuangEvaluationException($"you cannot do such operation 'nil {op.ToSymbol()}'");

        LinqExpression comparison;

        if (member.Type == typeof(string))
            comparison = LinqExpression.Call(IsNullOrEmptyMethod, member);
        else if (!member.Type.IsValueType || Nullable.GetUnderlyingType(member.Type) is not null)
            comparison = LinqExpression.Equal(member, LinqExpression.Constant(null, member.Type));
        else
            comparison = LinqExpression.Constant(false);

        return op == BinaryOperator.Eq ? comparison : LinqExpression.Not(comparison);
    }

    private LinqExpression BuildRegExpression(MemberExpression member, Expression value)
    {
        if (member.Type != typeof(string))
            throw new QuangEvaluationException("operator 'reg' is only valid for string fields.");

        if (value is not StringExpression pattern)
            throw new QuangEvaluationException("operator 'reg' requires a string pattern.");

        var patternConstant = LinqExpression.Constant(pattern.Value, typeof(string));

        var match = _regStrategy == RegStrategy.Contains
            ? LinqExpression.Call(member, ContainsMethod, patternConstant)
            : LinqExpression.Call(RegexIsMatchMethod, member, patternConstant);

        // an empty value never matches a pattern, and matching against null would throw
        return LinqExpression.AndAlso(
            LinqExpression.NotEqual(member, LinqExpression.Constant(null, typeof(string))),
            match);
    }

    private MemberExpression GetMemberExpression(SymbolExpression symbol)
    {
        var propertyName = MapSymbol(symbol.Value);

        var property = typeof(T).GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (property is null)
            throw new QuangEvaluationException($"the type {typeof(T).Name} does not have a public property named '{propertyName}'.");

        return LinqExpression.Property(_param, property);
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

    private ConstantExpression GetConstantExpression(MemberExpression member, Expression expr)
    {
        object value = expr switch
        {
            IntegerExpression i => i.Value,
            FloatExpression f => f.Value,
            BoolExpression b => b.Value,
            StringExpression s => s.Value,
            AtomExpression a => MapAtom(a.Value.Value),
            _ => throw new QuangEvaluationException($"unsupported constant type: {expr.DisplayName}")
        };

        return Coerce(member.Type, value);
    }

    /// <summary>
    /// Expression trees require both sides of a comparison to have the same type,
    /// so the literal is converted to the type of the field it is compared against.
    /// </summary>
    private static ConstantExpression Coerce(Type target, object value)
    {
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        if (underlying.IsInstanceOfType(value)) return LinqExpression.Constant(value, target);

        try
        {
            if (underlying.IsEnum)
            {
                var name = value as string;

                var converted = name is not null
                    ? Enum.Parse(underlying, name.TrimStart(':'), true)
                    : Enum.ToObject(underlying, value);

                return LinqExpression.Constant(converted, target);
            }

            if (value is IConvertible && typeof(IConvertible).IsAssignableFrom(underlying))
                return LinqExpression.Constant(Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture), target);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            throw new QuangEvaluationException($"cannot compare the value '{value}' against a field of type {underlying.Name}: {ex.Message}");
        }

        throw new QuangEvaluationException($"cannot compare the value '{value}' against a field of type {underlying.Name}");
    }
}
