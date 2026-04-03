if (args.Length != 1)
{
    Console.WriteLine("Usage: dotnet run -- <search-query>");

    return;
}

string[][] content = [.. File.ReadAllLines("./logs.txt")[1..].Select(line => line.Split(','))];

var quang = new Quang.Quang(args[0])
    .Init()
    .SetupAtom(":f", "f")
    .SetupAtom(":m", "m");

foreach (var line in content)
{
    var username = line[0].Trim();
    var age = int.Parse(line[1]);
    var sex = line[2].ToLower().Trim();
    var weight = float.Parse(line[3]);

    quang
        .AddStringVar("username", username)
        .AddAtomVar("sex", sex)
        .AddIntegerVar("age", age)
        .AddFloatVar("weight", weight);

    if (quang.Evaluate())
        Console.WriteLine($"Matched: {username},{age},{sex},{weight}");
}