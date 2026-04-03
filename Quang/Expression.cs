namespace Quang;

internal abstract record Expression;

internal record NilExpression() : Expression;
internal record BoolExpression(bool Value) : Expression;
internal record FloatExpression(float Value) : Expression;
internal record IntegerExpression(int Integer) : Expression;
internal record SymbolExpression(string Symbol) : Expression;
internal record AtomExpression(string Atom) : Expression;
internal record StringExpression(string String) : Expression;
internal record BinaryNode(BinaryExpression Binary) : Expression;
internal record BinaryExpression(Expression Left, BinaryOperator Operator, Expression Right) : Expression;

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
	Or,
}
