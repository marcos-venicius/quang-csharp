namespace Quang;

internal enum SyntaxExpressionValueType
{
    Nil,
    Bool,
    Float,
    Integer,
    Symbol,
    Atom,
    String
}

internal class TypeChecker
{
    private readonly Dictionary<string, SyntaxExpressionValueType> _symbols = [];
    private readonly HashSet<string> _atoms = [];

    internal TypeChecker(Dictionary<string, IExpressionValueTypeInfo> symbols, HashSet<string> atoms)
    {
        foreach (var kvp in symbols)
        {
            var typeInfo = kvp.Value;
            var kind = typeInfo.Type switch
            {
                Type t when t == typeof(NilExpression) => SyntaxExpressionValueType.Nil,
                Type t when t == typeof(BoolExpression) => SyntaxExpressionValueType.Bool,
                Type t when t == typeof(FloatExpression) => SyntaxExpressionValueType.Float,
                Type t when t == typeof(IntegerExpression) => SyntaxExpressionValueType.Integer,
                Type t when t == typeof(SymbolExpression) => SyntaxExpressionValueType.Symbol,
                Type t when t == typeof(AtomExpression) => SyntaxExpressionValueType.Atom,
                Type t when t == typeof(StringExpression) => SyntaxExpressionValueType.String,
                _ => throw new QuangTypeException($"Unsupported expression value type: {typeInfo.Type.Name}")
            };

            _symbols[kvp.Key] = kind;
        }

        _atoms = atoms;
    }

    public void Validate(Expression? expr)
    {
        if (expr is null) return;

        var type = GetExpressionType(expr);

        if (type != SyntaxExpressionValueType.Bool)
            throw new QuangTypeException($"the query must evaluate to a boolean, but it evaluates to {type.ToDisplayName()}.");
    }

    private SyntaxExpressionValueType GetExpressionType(Expression expr)
    {
        return expr switch
        {
            NilExpression => SyntaxExpressionValueType.Nil,
            IntegerExpression => SyntaxExpressionValueType.Integer,
            FloatExpression => SyntaxExpressionValueType.Float,
            StringExpression => SyntaxExpressionValueType.String,
            BoolExpression => SyntaxExpressionValueType.Bool,
            AtomExpression atom => ValidateAtom(atom),
            SymbolExpression s => ResolveSymbolType(s),
            BinaryExpression b => ValidateBinary(b),
            UnaryExpression u => ValidateUnary(u),
            _ => throw new QuangTypeException($"Unknown expression type: {expr.DisplayName}")
        };
    }

    private SyntaxExpressionValueType ValidateAtom(AtomExpression atom)
    {
        if (_atoms.Contains(atom.Value.Value))
            return SyntaxExpressionValueType.Atom;

        throw new QuangTypeException($"Atom '{atom.Value}' is not expected.");
    }

    private SyntaxExpressionValueType ResolveSymbolType(SymbolExpression symbol)
    {
        if (_symbols.TryGetValue(symbol.Value, out var type))
            return type;

        throw new QuangTypeException($"The variable '{symbol.Value}' is not defined in the current schema.");
    }

    private SyntaxExpressionValueType ValidateUnary(UnaryExpression unary)
    {
        var operandType = GetExpressionType(unary.Expr);

        return unary.Operator switch
        {
            UnaryOperator.Not =>
                operandType == SyntaxExpressionValueType.Bool
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangTypeException($"Unary operator '{unary.Operator}' requires a boolean operand, but got {operandType.ToDisplayName()}."),

            _ => throw new QuangTypeException($"Unsupported unary operator: {unary.Operator}")
        };
    }

    private SyntaxExpressionValueType ValidateBinary(BinaryExpression binary)
    {
        var leftType = GetExpressionType(binary.Left);
        var rightType = GetExpressionType(binary.Right);

        return binary.Operator switch
        {
            // Logical operators (AND/OR) require both sides to be Boolean
            BinaryOperator.And or BinaryOperator.Or =>
                (leftType == SyntaxExpressionValueType.Bool && rightType == SyntaxExpressionValueType.Bool)
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangTypeException($"Logical operator {binary.Operator} requires boolean operands."),

            // Equality: nil compares against anything, numbers are compared with each other,
            // everything else must match exactly.
            BinaryOperator.Eq or BinaryOperator.Ne =>
                leftType == SyntaxExpressionValueType.Nil
                || rightType == SyntaxExpressionValueType.Nil
                || leftType == rightType
                || (IsNumeric(leftType) && IsNumeric(rightType))
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangTypeException($"Cannot compare {leftType.ToDisplayName()} with {rightType.ToDisplayName()} using {binary.Operator}."),

            // Ordered comparisons (GT, LT, etc.) work on numbers and on strings
            BinaryOperator.Gt or BinaryOperator.Lt or BinaryOperator.Gte or BinaryOperator.Lte =>
                (IsNumeric(leftType) && IsNumeric(rightType))
                || (leftType == SyntaxExpressionValueType.String && rightType == SyntaxExpressionValueType.String)
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangTypeException($"Ordered comparison {binary.Operator} requires numeric or string operands, but got {leftType.ToDisplayName()} and {rightType.ToDisplayName()}."),

            // Regex requires String
            BinaryOperator.Reg =>
                (leftType == SyntaxExpressionValueType.String && rightType == SyntaxExpressionValueType.String)
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangTypeException($"Operator 'reg' is only valid for strings."),

            _ => throw new QuangTypeException($"Unsupported operator: {binary.Operator}")
        };
    }

    private static bool IsNumeric(SyntaxExpressionValueType type) =>
        type == SyntaxExpressionValueType.Integer || type == SyntaxExpressionValueType.Float;
}

internal static class SyntaxExpressionValueTypeExtensions
{
    internal static string ToDisplayName(this SyntaxExpressionValueType type) => type switch
    {
        SyntaxExpressionValueType.Nil => "nil",
        SyntaxExpressionValueType.Bool => "bool",
        SyntaxExpressionValueType.Float => "float",
        SyntaxExpressionValueType.Integer => "integer",
        SyntaxExpressionValueType.Symbol => "symbol",
        SyntaxExpressionValueType.Atom => "atom",
        SyntaxExpressionValueType.String => "string",
        _ => type.ToString().ToLowerInvariant(),
    };
}
