public sealed class QuangSyntaxException : ApplicationException
{
    public QuangSyntaxException(string message, int col) : base($"error 1:{col}: {message}")
    {}
}
