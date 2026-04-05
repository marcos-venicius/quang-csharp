namespace Quang;

public sealed class Quang(string query)
{
    private readonly string _query = query;
    private bool _initialized = false;
    private Expression? _expression;
    private readonly Dictionary<string, IExpressionValueTypeInfo> _expectedSymbols = [];
    internal readonly HashSet<string> _expectedAtoms = [];

    public Quang Init()
    {
        var lexer = new Lexer(_query);
        var tokens = lexer.Lex();

        var parser = new Parser(tokens);
        _expression = parser.Parse();

        _initialized = true;
        return this;
    }

    /// <summary>
    /// This allows you to specify the expected symbols in the expression, and their types.
    /// So, if for example you want to allow the user to use a variable "agent" in the expression, you can specify that "agent" is a string variable, so when the
    /// user types "agent" in the expression, the language will know that "agent" is a string variable, and it will be able to apply string operations on it, like regex matching, or string comparison.
    /// </summary>
    public Quang SyntaxExpectSymbol(string name, IExpressionValueTypeInfo valueType)
    {
        _expectedSymbols.Add(name, valueType);

        return this;
    }

    /// <summary>
    /// This allows you to specify the expected atoms.
    /// So, if for example you wan to allow only http method atoms like :post, :delete, and :get, if the user
    /// specifies any other atom like :put, the language will throw an exception because that atom is not expected.
    /// </summary>
    public Quang SyntaxExpectAtom(string atom)
    {
        _expectedAtoms.Add(atom);

        return this;
    }

    /// <summary>
    /// Returns the evaluator so you can evaluate the expression given a context.
    /// You must call Init() before calling this method, otherwise an exception will be thrown because
    /// the expression tree is not built yet.
    /// </summary>
    public Evaluator Evaluator()
    {
        if (!_initialized)
            throw new InvalidOperationException("Quang must be initialized before evaluation.");

        var typeChecker = new TypeChecker(_expectedSymbols, _expectedAtoms);

        typeChecker.Validate(_expression);

        return new Evaluator(_expression, _expectedAtoms);
    }

    internal Expression? GetExpressionTree()
    {
        if (!_initialized)
            throw new InvalidOperationException("Quang must be initialized before type checking.");

        var typeChecker = new TypeChecker(_expectedSymbols, _expectedAtoms);

        typeChecker.Validate(_expression);

        return _expression;
    }
}
