namespace Quang;

internal abstract record Expression(string DisplayName)
{
	public override string ToString() => DisplayName;
};

internal record NilExpression() : Expression("nil");
internal record BoolExpression(bool Value) : Expression("bool");
internal record FloatExpression(float Value) : Expression("float");
internal record IntegerExpression(int Value) : Expression("integer");
internal record SymbolExpression(string Value) : Expression("symbol");
internal record AtomExpression(Atom Value) : Expression("atom");
internal record StringExpression(string Value) : Expression("string");
internal record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right) : Expression("binary");

internal enum BinaryOperator
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