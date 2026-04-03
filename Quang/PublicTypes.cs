namespace Quang;

public readonly struct Atom(string value)
{
    private readonly string Value { get; } = value;

    public static explicit operator Atom(string value) => new(value);
    public static explicit operator string(Atom atom) => atom.Value;

    public override string ToString() => Value;
}