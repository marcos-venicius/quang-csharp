namespace Quang;

public sealed class Quang(string query)
{
    private readonly string _query = query;
    private bool _initialized = false;
    private Evaluator _evaluator = null!;

    public Quang Init()
    {
        var lexer = new Lexer(_query);
        var tokens = lexer.Lex();

        var parser = new Parser(tokens);
        var expr = parser.Parse();

        _evaluator = new Evaluator(expr);

        _initialized = true;
        return this;
    }

    /// <summary>
    /// Build the set of available atoms.
    /// [You only need to do this once]
    /// Atoms works just like enums. You can say that an atom ":get" is 0
    /// So, everytime the user types ":get" in the query, i'll be substituted by 0,
    /// That way you make it easier to the user by specify a variable "kind", instead of typing 0, he types :get
    /// </summary>
    public Quang SetupAtoms(Dictionary<string, Atom> atoms)
    {
        foreach (var kvp in atoms)
            SetupAtom(kvp.Key, kvp.Value);

        return this;
    }

    /// <summary>
    /// Build the set of available atoms.
    /// [You only need to do this once]
    /// Atoms works just like enums. You can say that an atom ":get" is 0
    /// So, everytime the user types ":get" in the query, i'll be substituted by 0,
    /// That way you make it easier to the user by specify a variable "kind", instead of typing 0, he types :get
    /// </summary>
    public Quang SetupAtom(string name, Atom value)
    {
        _evaluator.SetAtomValue(name, value);

        return this;
    }

    /// <summary>
    /// For each evaluation, you can provide different variable values.
    /// If, for example you want to do a query over a bunch of logs the user
    /// will provide the query, for example filtering by a specific user agent pattern
    /// then, for each log row, you can update the "agent" variable value to the current log row user agent
    /// so the, when the language lazy evaluate the "agent" variable the query will be applied to the current
    /// log row successfully
    /// </summary>
    public Quang AddStringVar(string name, string value)
    {
        _evaluator.AddStringVar(name, value);

        return this;
    }

    /// <summary>
    /// For each evaluation, you can provide different variable values.
    /// If, for example you want to do a query over a bunch of logs the user
    /// will provide the query, for example filtering by a specific user agent pattern
    /// then, for each log row, you can update the "agent" variable value to the current log row user agent
    /// so the, when the language lazy evaluate the "agent" variable the query will be applied to the current
    /// log row successfully
    /// </summary>
    public Quang AddIntegerVar(string name, int value)
    {
        _evaluator.AddIntegerVar(name, value);

        return this;
    }


    /// <summary>
    /// For each evaluation, you can provide different variable values.
    /// If, for example you want to do a query over a bunch of logs the user
    /// will provide the query, for example filtering by a specific user agent pattern
    /// then, for each log row, you can update the "agent" variable value to the current log row user agent
    /// so the, when the language lazy evaluate the "agent" variable the query will be applied to the current
    /// log row successfully
    /// </summary>
    public Quang AddFloatVar(string name, float value)
    {
        _evaluator.AddFloatVar(name, value);

        return this;
    }

    /// <summary>
    /// For each evaluation, you can provide different variable values.
    /// If, for example you want to do a query over a bunch of logs the user
    /// will provide the query, for example filtering by a specific user agent pattern
    /// then, for each log row, you can update the "agent" variable value to the current log row user agent
    /// so the, when the language lazy evaluate the "agent" variable the query will be applied to the current
    /// log row successfully
    /// </summary>
    public Quang AddBoolVar(string name, bool value)
    {
        _evaluator.AddBoolVar(name, value);

        return this;
    }

    /// <summary>
    /// For each evaluation, you can provide different variable values.
    /// If, for example you want to do a query over a bunch of logs the user
    /// will provide the query, for example filtering by a specific user agent pattern
    /// then, for each log row, you can update the "agent" variable value to the current log row user agent
    /// so the, when the language lazy evaluate the "agent" variable the query will be applied to the current
    /// log row successfully
    /// </summary>
    public Quang AddAtomVar(string name, Atom value)
    {
        _evaluator.AddAtomVar(name, value);

        return this;
    }

    public bool Evaluate()
    {
        if (!_initialized)
            throw new InvalidOperationException("Quang must be initialized before evaluation.");

        return _evaluator.Evaluate();
    }
}