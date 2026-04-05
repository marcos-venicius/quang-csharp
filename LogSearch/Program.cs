using Quang;

if (args.Length != 1)
{
    Console.WriteLine("Usage: dotnet run -- <search-query>");

    return;
}

string[][] content = [.. File.ReadAllLines("./logs.txt")[1..].Select(line => line.Split(','))];

var quang = new Quang.Quang(args[0])
    .Init()
    .SyntaxExpectAtom(":f")
    .SyntaxExpectAtom(":m")
    .SyntaxExpectSymbol("age", new ExpressionValueTypeInfo<IntegerExpression>())
    .SyntaxExpectSymbol("weight", new ExpressionValueTypeInfo<FloatExpression>())
    .SyntaxExpectSymbol("username", new ExpressionValueTypeInfo<StringExpression>())
    .SyntaxExpectSymbol("sex", new ExpressionValueTypeInfo<AtomExpression>());

foreach (var line in content)
{
    var username = line[0].Trim();
    var age = int.Parse(line[1]);
    var sex = line[2].ToLower().Trim();
    var weight = float.Parse(line[3]);

    var evaluator = quang.Evaluator()
        .AddStringVar("username", username)
        .AddAtomVar("sex", $":{sex}")
        .AddIntegerVar("age", age)
        .AddFloatVar("weight", weight);

    if (evaluator.Evaluate())
        Console.WriteLine($"Matched: {username},{age},{sex},{weight}");
}