namespace Quang;

public abstract record Expression(string DisplayName)
{
	public override string ToString() => DisplayName;
};

public abstract record ExpressionValueType(string DisplayName) : Expression(DisplayName);

public record NilExpression() : ExpressionValueType("nil");
public record BoolExpression(bool Value) : ExpressionValueType("bool");
public record FloatExpression(float Value) : ExpressionValueType("float");
public record IntegerExpression(int Value) : ExpressionValueType("integer");
public record SymbolExpression(string Value) : ExpressionValueType("symbol");
public record StringExpression(string Value) : ExpressionValueType("string");
public record AtomExpression(Atom Value) : ExpressionValueType("atom");
public record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right) : Expression("binary");

public enum BinaryOperator
{
	Eq,
	Ne,
	Gt,
	Lt,
	Gte,
	Lte,
	Reg,
	And,
	Or
}

internal static class BinaryOperatorExtensions
{
	public static string ToSymbol(this BinaryOperator op) => op switch
	{
		BinaryOperator.Eq => "eq",
		BinaryOperator.Ne => "ne",
		BinaryOperator.Gt => "gt",
		BinaryOperator.Lt => "lt",
		BinaryOperator.Gte => "gte",
		BinaryOperator.Lte => "lte",
		BinaryOperator.Reg => "reg",
		BinaryOperator.And => "and",
		BinaryOperator.Or => "or",
		_ => throw new QuangSyntaxException($"unknown binary operator {op}", 1),
	};
}
