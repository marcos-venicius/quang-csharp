namespace Quang;

/// <summary>
/// Base type for every error raised by the language.
/// <see cref="Line"/> and <see cref="Column"/> are filled whenever the error can be
/// tied back to a position in the query, and are null otherwise.
/// </summary>
public class QuangException : Exception
{
    public int? Line { get; }
    public int? Column { get; }

    public QuangException(string message) : base($"error: {message}")
    { }

    public QuangException(string message, int line, int column) : base($"error {line}:{column}: {message}")
    {
        Line = line;
        Column = column;
    }
}

/// <summary>
/// The query could not be lexed or parsed.
/// </summary>
public sealed class QuangSyntaxException : QuangException
{
    public QuangSyntaxException(string message) : base(message)
    { }

    public QuangSyntaxException(string message, int line, int column) : base(message, line, column)
    { }
}

/// <summary>
/// The query is syntactically valid but does not type check against the declared schema.
/// </summary>
public sealed class QuangTypeException : QuangException
{
    public QuangTypeException(string message) : base(message)
    { }

    public QuangTypeException(string message, int line, int column) : base(message, line, column)
    { }
}

/// <summary>
/// The query could not be evaluated (missing variable, incompatible values, invalid regex, ...).
/// </summary>
public sealed class QuangEvaluationException : QuangException
{
    public QuangEvaluationException(string message) : base(message)
    { }

    public QuangEvaluationException(string message, int line, int column) : base(message, line, column)
    { }
}
