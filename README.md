# Quang

[![.NET](https://github.com/marcos-venicius/quang-csharp/actions/workflows/dotnet.yml/badge.svg)](https://github.com/marcos-venicius/quang-csharp/actions/workflows/dotnet.yml)

`Quang` is meant to be a "query" language for your tools.

`Quang` extends for `Query Language`.

You can use the language to implement filters in your applications without the need of tons of flags to your user,
just provide a simple and consise integrated language and let the user build the filter himself.

So, it is at the end a _"language as a lib"_.

_There is a full [playlist on youtube](https://youtube.com/playlist?list=PL3YefAkg_zCgPTINetXJ7Aatpps2DECcW&si=_jBBPe7s6Ne4B70s) creating this project._

## Api and language details

Does not matter what kind of query you provide as input to the evaluator, it will always return `true` or `false`. If the query is empty, it will always return `true`.

**Data Types**

| name     | supported | format                       | description                                                                   |
| -------- | --------- | ---------------------------- | ----------------------------------------------------------------------------- |
| Integers | yes       | `[0-9]+`                     | 64 bit signed integers (`long`)                                               |
| Atoms    | yes       | `:[a-zA-Z_][a-zA-Z0-9_]*`    | it works like enumerators                                                     |
| String   | yes       | `'.*'`                       | you can scape string with `\'`, any other backslash is kept (`'ML-\d+'`)      |
| Boolean  | yes       | `true\|false`                |                                                                               |
| Nil      | yes       | `nil`                        | represents all kinds of empty values ("", nil) (zero is not considered empty) |
| Floats   | yes       | `\d+\.\d*`                   | 64 bit floats (`double`)                                                      |

Variable names follow the same shape as atom names: `[a-zA-Z_][a-zA-Z0-9_]*`.
Numbers are always read with the invariant culture, so `70.2` means the same thing on every machine.
Whitespace (including line breaks) is free, so a long query can be written across multiple lines.

**Keywords**

| name     | description   | usage            |
| -------- | ------------- | ---------------- |
| not      | negation      | `not false`      |
| and      | logical and   | `true and true`  |
| or       | logical or    | `true or false`  |
| nil      | null value    | `name eq nil`    |
| true     | boolean true  | `alive eq true`  |
| false    | boolean false | `alive eq false` |

**Operators**

| name     | description                                                                                        | example                 |
| -------- | -------------------------------------------------------------------------------------------------- | ----------------------- |
| not      | negate boolean expressions                                                                         | `not status eq 400`     |
| eq       | check if `a` is equal to `b`. (Integers, Floats, Strings, Booleans, Nils, Atoms)                   | `a eq b`                |
| ne       | check if `a` is not equal to `b`. (Integers, Floats, Strings, Booleans, Nils, Atoms)               | `a ne b`                |
| lt       | check if `a` is less than `b`. (Integers, Floats, Strings)                                         | `a lt b`                |
| gt       | check if `a` is greater than `b`. (Integers, Floats, Strings)                                      | `a gt b`                |
| lte      | check if `a` is less than or equal to `b`. (Integers, Floats, Strings)                             | `a lte b`               |
| gte      | check if `a` is greater than or equal to `b`. (Integers, Floats, Strings)                          | `a gte b`               |
| reg      | check if `a` matches pattern `b`. `b` accepts valid regex. `a` should be a string                  | `a reg b`               |

Types are strict, with two exceptions: integers and floats are compared with each other
(`age lt 30.5`), and `nil` can be compared against any value.

**Precedence**

From the tightest to the loosest: comparison, `not`, `and`, `or`.
So `not status eq 400 and active` means `(not (status eq 400)) and active`.

**Nil**

`nil` is the empty value. `name eq nil` is true when `name` was declared as nil
(`AddNilVar`, or a null value passed to `AddStringVar`) or when it holds an empty string.
Zero, `false` and empty atoms are **not** empty. An empty value never matches a `reg` pattern.

**Booleans**

A boolean variable is a query by itself, so `active`, `not active` and `active and status eq 200`
are all valid, as well as the explicit `active eq true`.

**Basic syntax**

Pretend we have a list of computers that have the following properties:

- Identifier
- Running
- Cors

So, we could query something like:

```elixir
(running eq true and cors gte 4 and cors lte 10) or (running eq false and identifier reg 'ML-\d+') or identifier eq nil
```

> [!TIP]
> This repository is a port of [Quang](https://github.com/marcos-venicius/quang) for CSharp. See the original repository for more information.

> [!NOTE]
> Building a natural language search on top of Quang? [LLM.md](./LLM.md) is a complete
> specification of the language written to be pasted into a model's context, with a prompt
> template, worked examples, the mistakes models usually make, and the full error catalog.

# How to Use

Let's pretend you have a list of people and you need to build a CLI to search this list.

```
username,age,sex,weight
user001,25,M,70.2
user002,31,F,60.7
user003,22,M,68.4
user004,29,F,55.9
user005,35,M,80.1
user006,28,F,62.5
user007,24,M,72.3
user008,30,F,59.6
user009,27,M,75.8
user010,33,F,65.2
user011,26,M,71.4
user012,32,F,63.7
user013,23,M,69.9
user014,34,F,58.3
```

Then, you can with a **less than 20 lines of code** integrate the language with your cli.

Here is a **fully function example**:

```csharp
using System.Globalization;

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

// the query is type checked once, so build the evaluator outside of the loop
// and just change the variables for each row
var evaluator = quang.Evaluator();

foreach (var line in content)
{
    var username = line[0].Trim();
    var age = int.Parse(line[1], CultureInfo.InvariantCulture);
    var sex = line[2].ToLower().Trim();
    var weight = double.Parse(line[3], CultureInfo.InvariantCulture);

    evaluator
        .AddStringVar("username", username)
        .AddAtomVar("sex", $":{sex}")
        .AddIntegerVar("age", age)
        .AddFloatVar("weight", weight);

    if (evaluator.Evaluate())
        Console.WriteLine($"Matched: {username},{age},{sex},{weight}");
}
```

In fact, this example is present [here](./LogSearch/Program.cs).

Then, if you run and use this filter: `dotnet run -- 'sex eq :m and weight lte 70.0 and age gte 23'`, it should return you this:

```
Matched: user013,23,m,69.9
```

**✨ Is that easy!**

# Querying with LINQ

Instead of evaluating row by row, you can translate a query into an `Expression<Func<T, bool>>`
and hand it to LINQ, EF Core, or anything else that takes a predicate:

```csharp
using Quang;
using Quang.Interpreters;

var quang = new Quang.Quang("Age gte 18 and Name reg '^A'")
    .Init()
    .SyntaxExpectSymbol("Age", new ExpressionValueTypeInfo<IntegerExpression>())
    .SyntaxExpectSymbol("Name", new ExpressionValueTypeInfo<StringExpression>());

var predicate = new LinqInterpreter<User>().Translate(quang);

var result = users.Where(predicate.Compile()).ToList();
```

The interpreter maps every variable to a public property of `T` (case insensitive), and converts
the literal to the property type, so an `Integer` in the query works against `int`, `long`,
`decimal`, `int?` and so on. Atoms can be mapped to enum values or to any string you want.

```csharp
new LinqInterpreter<User>(
    symbolsMapping: new() { { "idade", "Age" } },   // query name -> property name
    atomsMapping:   new() { { ":m", "M" } },        // atom -> stored value
    regStrategy:    RegStrategy.Contains);          // how "reg" is translated
```

`reg` is translated to `Regex.IsMatch` by default, which behaves exactly like the evaluator.
Use `RegStrategy.Contains` when the predicate goes to a database provider like EF Core, since
`Contains` becomes a SQL `LIKE` while a regex would have to run in memory.

**Errors**

Every error thrown by the language derives from `QuangException`, which carries the `Line` and
`Column` of the problem whenever it is known:

- `QuangSyntaxException`: the query could not be lexed or parsed.
- `QuangTypeException`: the query does not match the declared schema.
- `QuangEvaluationException`: the query could not be evaluated (missing variable, invalid regex, ...).
