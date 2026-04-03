namespace Quang;

public readonly struct Atom(string value)
{
    private readonly string Value { get; } = value;

    public static implicit operator Atom(string value) => new(value);
    public static implicit operator string(Atom atom) => atom.Value;

    public override string ToString() => Value;
}