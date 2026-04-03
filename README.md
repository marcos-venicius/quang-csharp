# Quang

`Quang` is meant to be a "query" language for your tools.

`Quang` extends for `Query Language`.

You can use the language to implement filters in your applications without the need of tons of flags to your user,
just provide a simple and consise integrated language and let the user build the filter himself.

So, it is at the end a _"language as a lib"_.

_There is a full [playlist on youtube](https://youtube.com/playlist?list=PL3YefAkg_zCgPTINetXJ7Aatpps2DECcW&si=_jBBPe7s6Ne4B70s) creating this project._

## Api and language details

Does not matter what kind of query you provide as input to the evaluator, it will always return `true` or `false`. If the query is empty, it will always return `true`.

**Data Types**

| name     | supported | format        | description                                                                   |
| -------- | --------- | ------------- | ----------------------------------------------------------------------------- |
| Integers | yes       | `[0-9]+`      | golang 64bit signed integers                                                  |
| Atoms    | yes       | `:[a-zA-Z_]+` | it works like enumerators                                                     |
| String   | yes       | `'.*'`        | you can scape string with `\'`                                                |
| Boolean  | yes       | `true\|false` |                                                                               |
| Nil      | yes       | `nil`         | represents all kinds of empty values ("", nil) (zero is not considered empty) |
| Floats   | yes       | `\d+\.\d*`    | golang 64bit floats                                                           |

**Keywords**

| name     | description   | usage            |
| -------- | ------------- | ---------------- |
| and      | logical and   | `true and true`  |
| or       | logical or    | `true or false`  |
| nil      | null value    | `name eq nil`    |
| true     | boolean true  | `alive eq true`  |
| false    | boolean false | `alive eq false` |

**Operators**

| name     | description                                                                                        | example                 |
| -------- | -------------------------------------------------------------------------------------------------- | ----------------------- |
| eq       | check if `a` is equal to `b`. strict types. (Integers, Strings, Booleans, Nils, Floats, Atoms)     | `a eq b`                |
| ne       | check if `a` is not equal to `b`. strict types. (Integers, Strings, Booleans, Nils, Floats, Atoms) | `a ne b`                |
| lt       | check if `a` is less than `b`. strict types. (Integers, Strings)                                   | `a lt b`                |
| gt       | check if `a` is greater than `b`. strict types. (Integers, Strings)                                | `a gt b`                |
| lte      | check if `a` is less than or equal to `b`. strict types. (Integers, Strings)                       | `a lte b`               |
| gte      | check if `a` is greater than or equal to `b`. strict types. (Integers, Strings)                    | `a gte b`               |
| reg      | check if `a` matches pattern `b`. `b` accepts valid regex. `a` should be a string                  | `a reg b`               |

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
```

In fact, this example is present [here](./LogSearch/Program.cs).

Then, if you run and use this filter: `dotnet run -- 'sex eq :m and weight lte 70.0 and age gte 23'`, it should return you this:

```
Matched: user013,23,m,69.9
```

**✨ Is that easy!**