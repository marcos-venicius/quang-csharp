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
                _ => throw new QuangSyntaxException($"Unsupported expression value type: {typeInfo.Type.Name}")
            };

            _symbols[kvp.Key] = kind;
        }

        _atoms = atoms;
    }

    public void Validate(Expression? expr)
    {
        if (expr != null) GetExpressionType(expr);
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
            _ => throw new QuangSyntaxException($"Unknown expression type: {expr.DisplayName}")
        };
    }

    private SyntaxExpressionValueType ValidateAtom(AtomExpression atom)
    {
        if (_atoms.Contains(atom.Value))
            return SyntaxExpressionValueType.Atom;

        throw new QuangSyntaxException($"Atom '{atom.Value}' is not expected.");
    }

    private SyntaxExpressionValueType ResolveSymbolType(SymbolExpression symbol)
    {
        if (_symbols.TryGetValue(symbol.Value, out var type))
            return type;

        throw new QuangSyntaxException($"The variable '{symbol.Value}' is not defined in the current schema.");
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
                    : throw new QuangSyntaxException($"Logical operator {binary.Operator} requires boolean operands."),

            // Comparisons require matching types
            BinaryOperator.Eq or BinaryOperator.Ne =>
                leftType == SyntaxExpressionValueType.Nil || rightType == SyntaxExpressionValueType.Nil || (leftType == rightType)
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangSyntaxException($"Cannot compare {leftType} with {rightType} using {binary.Operator}."),

            // Ordered comparisons (GT, LT, etc.) require Numeric types
            BinaryOperator.Gt or BinaryOperator.Lt or BinaryOperator.Gte or BinaryOperator.Lte =>
                (IsNumeric(leftType) && IsNumeric(rightType))
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangSyntaxException($"Ordered comparison {binary.Operator} requires numeric types."),

            // Regex/Contains requires String
            BinaryOperator.Reg =>
                (leftType == SyntaxExpressionValueType.String && rightType == SyntaxExpressionValueType.String)
                    ? SyntaxExpressionValueType.Bool
                    : throw new QuangSyntaxException($"Operator 'reg' is only valid for strings."),

            _ => throw new QuangSyntaxException($"Unsupported operator: {binary.Operator}")
        };
    }

    private static bool IsNumeric(SyntaxExpressionValueType type) =>
        type == SyntaxExpressionValueType.Integer || type == SyntaxExpressionValueType.Float;
}