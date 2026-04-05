namespace Quang;

public readonly struct Atom(string value)
{
    private readonly string Value { get; } = value;

    public static implicit operator Atom(string value) => new(value);
    public static implicit operator string(Atom atom) => atom.Value;

    public override string ToString() => Value;
}

public interface IExpressionValueTypeInfo
{
    public Type Type { get; }
}

public sealed class ExpressionValueTypeInfo<T> : IExpressionValueTypeInfo where T : ExpressionValueType
{
    public Type Type => typeof(T);
}