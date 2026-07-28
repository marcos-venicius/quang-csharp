namespace Quang;

/// <summary>
/// An atom works like an enumerator value. It is always written as ":name",
/// where name starts with a letter or underscore and may contain digits.
/// </summary>
public readonly struct Atom : IEquatable<Atom>
{
    private readonly string? _value;

    public Atom(string value)
    {
        Validate(value);

        _value = value;
    }

    public string Value => _value ?? string.Empty;

    public static implicit operator Atom(string value) => new(value);
    public static implicit operator string(Atom atom) => atom.Value;

    public bool Equals(Atom other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is Atom other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;

    public static bool operator ==(Atom left, Atom right) => left.Equals(right);
    public static bool operator !=(Atom left, Atom right) => !left.Equals(right);

    internal static void Validate(string value)
    {
        if (string.IsNullOrEmpty(value) || value[0] != ':' || value.Length < 2)
            throw new QuangException($"invalid atom \"{value}\": atoms must start with ':' followed by a name, like \":get\"");

        for (var i = 1; i < value.Length; i++)
        {
            var chr = value[i];
            var isLetter = (chr >= 'a' && chr <= 'z') || (chr >= 'A' && chr <= 'Z') || chr == '_';
            var isDigit = chr >= '0' && chr <= '9';

            if (isLetter || (isDigit && i > 1)) continue;

            throw new QuangException($"invalid atom \"{value}\": atoms must match ':[a-zA-Z_][a-zA-Z0-9_]*'");
        }
    }
}

public interface IExpressionValueTypeInfo
{
    public Type Type { get; }
}

public sealed class ExpressionValueTypeInfo<T> : IExpressionValueTypeInfo where T : ExpressionValueType
{
    public Type Type => typeof(T);
}
