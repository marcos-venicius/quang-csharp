# Quang for LLMs

A complete, self-contained specification of the Quang query language, written to be pasted into a
model's context. If you are a language model asked to write a Quang query, everything you need is
here: the grammar, the type rules, the error messages, and the mistakes to avoid.

Quang is a **filter language**. A query is a boolean expression over a fixed set of fields that the
host application declares. Evaluating a query against one record always answers `true` or `false`.
There is no projection, no ordering, no aggregation, no arithmetic and no assignment.

---

## 1. Output contract

When you are asked to produce a query:

1. Output **one single-line query and nothing else** — no quotes around it, no markdown fence, no
   explanation, unless you were explicitly asked for one.
2. Use **only the field names and atoms listed in the schema** you were given. Any other name is a
   hard error, not a guess. If the request needs a field that does not exist, say so instead of
   inventing one.
3. The whole query must be a boolean. `age gt 18` is a query; `age` is only a query when `age` is a
   boolean field; `42` is never a query.
4. If the request has no filter at all, output an **empty query** (the empty string). An empty query
   matches everything.
5. Never invent operators. The complete list is `eq ne gt lt gte lte reg not and or`.

---

## 2. Grammar

```ebnf
query      = [ expression ] ;                     (* empty query is valid and matches everything *)
expression = term { "or" term } ;
term       = factor { "and" factor } ;
factor     = "not" factor | comparison ;
comparison = primary [ cmp_op primary ] ;         (* not associative: a eq b eq c is invalid *)
primary    = "(" expression ")" | literal | field ;
cmp_op     = "eq" | "ne" | "gt" | "lt" | "gte" | "lte" | "reg" ;

literal    = integer | float | string | atom | "true" | "false" | "nil" ;
integer    = digit { digit } ;                    (* 64 bit signed, no sign, no separators *)
float      = digit { digit } "." { digit } ;      (* 64 bit, no exponent, digit required before "." *)
string     = "'" { any_char | "\\'" | "\\\\" } "'" ;
atom       = ":" letter_or_underscore { letter_or_underscore | digit } ;
field      = letter_or_underscore { letter_or_underscore | digit } ;
```

Whitespace (spaces, tabs, line breaks) separates tokens and is otherwise ignored, so a long query
may span several lines. Everything is **case sensitive**: keywords are lowercase (`AND` is not
`and`), and field names must match the schema exactly (`AGE` is not `age`).

Reserved words that can never be used as field names:
`true`, `false`, `nil`, `and`, `or`, `not`, `eq`, `ne`, `gt`, `lt`, `gte`, `lte`, `reg`.

---

## 3. Types

| Type    | Literal syntax              | Notes                                                        |
| ------- | --------------------------- | ------------------------------------------------------------ |
| Integer | `0`, `42`, `9007199254740`  | 64 bit signed. **No negative literals**, no `_`, no `1e5`.   |
| Float   | `0.0`, `70.2`, `10.`        | 64 bit. A digit is required before the dot: `0.5`, not `.5`. |
| String  | `'hello'`, `'it\'s'`        | Single quotes only. `\'` and `\\` are escapes; every other backslash is literal, so `'ML-\d+'` is a valid regex pattern. |
| Boolean | `true`, `false`             |                                                              |
| Atom    | `:get`, `:m`, `:user_2`     | An enum-like value. Must be declared by the host.            |
| Nil     | `nil`                       | The empty value.                                             |

Negative numbers are a syntax error. To express "below zero", compare against a field or rewrite
the filter (`not amount gte 0`).

---

## 4. Operators and precedence

| Operator | Meaning                      | Valid operand types                                  |
| -------- | ---------------------------- | ---------------------------------------------------- |
| `eq`     | equal                        | any two values of the same type; integer↔float; anything against `nil` |
| `ne`     | not equal                    | same as `eq`                                         |
| `gt`     | greater than                 | Integer, Float, String                               |
| `lt`     | less than                    | Integer, Float, String                               |
| `gte`    | greater than or equal        | Integer, Float, String                               |
| `lte`    | less than or equal           | Integer, Float, String                               |
| `reg`    | regex match, `field reg 'p'` | String on both sides                                 |
| `not`    | negation                     | boolean expression                                   |
| `and`    | conjunction                  | boolean expressions                                  |
| `or`     | disjunction                  | boolean expressions                                  |

Precedence, tightest first:

1. `(` ... `)`
2. comparison (`eq ne gt lt gte lte reg`)
3. `not`
4. `and`
5. `or`

So `not status eq 400 and active or size gt 10` means
`(((not (status eq 400)) and active) or (size gt 10))`.

Comparisons are **not associative**: `age gt 18 eq true` is a syntax error. Wrap it:
`(age gt 18) eq true`.

There is no `in`, `between`, `like`, `contains`, `startswith`, `+`, `-`, `*`, `/`, `%`, or `null`.
Use `or` for a set membership (`method eq :get or method eq :post`) and `reg` for text search.

---

## 5. Semantics

**Numbers.** Integers and floats compare with each other: `age lt 30.5` and `weight gte 70` are
both valid.

**Fields on both sides.** A comparison does not need a literal. `size gt latency` compares two
fields of the same record, and works for any two fields of comparable types (numbers with numbers,
strings with strings). The same is true for `reg`, where the pattern itself may be a field.

**Strings.** Comparison is ordinal (UTF-16 code unit order), so uppercase letters sort before
lowercase ones: `'Z' lt 'a'` is true. Equality is exact and case sensitive.

**Regex.** `field reg 'pattern'` uses .NET regex and is an unanchored search: `name reg 'user'`
matches anywhere in the value. Anchor with `^` and `$` when you mean the whole value. Inline
options work, so `name reg '(?i)^user'` is a case insensitive match. The left side must be the
string field, the right side the pattern.

**Booleans.** A boolean field is a query by itself. All of these are valid and equivalent to each
other: `active`, `active eq true`, `not (active eq false)`. Prefer the short form `active` and
`not active`.

**Nil.** `nil` means "empty". `field eq nil` is true when the field is null **or an empty string**.
Zero, `false` and `0.0` are *not* empty. Write `nil` only with `eq` and `ne`: using it with `gt`,
`lt`, `gte`, `lte` or `reg` is a type error. At run time, a field that happens to be empty simply
never matches a `reg` pattern — that is `false`, not an error.

**Atoms.** An atom is compared only with `eq` and `ne`, and only against another atom:
`method eq :get`. Atoms cannot be ordered and cannot be regex-matched. Only atoms declared in the
schema may appear in a query.

**Empty query.** An empty query is valid and matches every record. Use it when the request asks for
"everything".

---

## 6. The schema

The host application declares which fields exist, their types, and which atoms are allowed. That
declaration is the contract you must respect. A schema is usually presented to you like this:

```
Fields:
  status   integer   HTTP status code
  size     integer   response size in bytes
  latency  float     seconds
  path     string    request path
  agent    string    user agent
  method   atom      one of :get, :post, :put, :delete
  cached   bool      whether the response came from cache
Atoms: :get :post :put :delete
```

Rules that follow from a schema:

- A name that is not listed is an error: `The variable 'x' is not defined in the current schema.`
- An atom that is not listed is an error: `Atom ':patch' is not expected.`
- The declared type decides which operators are legal: `status reg '4..'` is invalid because
  `status` is an integer, not a string.

---

## 7. Worked examples

Using the schema above:

| Request                                              | Query                                                        |
| ---------------------------------------------------- | ------------------------------------------------------------ |
| errors only                                          | `status gte 400`                                             |
| successful GETs                                      | `method eq :get and status gte 200 and status lt 300`         |
| everything except redirects                          | `not (status gte 300 and status lt 400)`                      |
| GET or POST                                          | `method eq :get or method eq :post`                           |
| slow requests over 1.5s                              | `latency gt 1.5`                                             |
| API paths                                            | `path reg '^/api/'`                                          |
| paths that look like `/users/<id>`                   | `path reg '^/users/\d+$'`                                    |
| requests from curl, case insensitive                 | `agent reg '(?i)curl'`                                       |
| requests with no user agent                          | `agent eq nil`                                               |
| requests that do have a user agent                   | `agent ne nil`                                               |
| cached responses                                     | `cached`                                                     |
| responses that were not cached                       | `not cached`                                                 |
| big and slow                                         | `size gt 1000000 and latency gt 2.0`                          |
| errors on the API, or anything really slow           | `(path reg '^/api/' and status gte 500) or latency gt 10.0`   |
| status between 200 and 299 inclusive                 | `status gte 200 and status lte 299`                           |
| no filter at all                                     | *(empty query)*                                              |

---

## 8. Common mistakes

| Wrong                          | Why                                              | Right                                     |
| ------------------------------ | ------------------------------------------------ | ----------------------------------------- |
| `status = 200`                 | `=` does not exist                               | `status eq 200`                           |
| `status != 200`                | `!=` does not exist                              | `status ne 200`                           |
| `status > 400`                 | `>` does not exist                               | `status gt 400`                           |
| `status >= 400 && cached`      | `&&`, `||`, `!` do not exist                     | `status gte 400 and cached`               |
| `NOT (status eq 400)`          | keywords are lowercase                           | `not (status eq 400)`                     |
| `status in (200, 201)`         | no `in` operator                                 | `status eq 200 or status eq 201`          |
| `status between 200 and 299`   | no `between`                                     | `status gte 200 and status lte 299`       |
| `path like '%api%'`            | no `like`, no `%` wildcards                      | `path reg 'api'`                          |
| `path eq "/api"`               | double quotes are not strings                    | `path eq '/api'`                          |
| `amount gt -5`                 | no negative literals                             | `not amount lte 0` (or rephrase)          |
| `latency gt .5`                | a digit is required before the dot               | `latency gt 0.5`                          |
| `size gt 1e6`                  | no exponent notation                             | `size gt 1000000`                         |
| `method eq 'get'`              | an atom field needs an atom, not a string        | `method eq :get`                          |
| `method eq :patch`             | the atom is not in the schema                    | use a declared atom                       |
| `status reg '^4'`              | `reg` needs a string field                       | `status gte 400 and status lt 500`        |
| `agent eq ''`                  | works, but `nil` is the documented empty check   | `agent eq nil`                            |
| `status eq 200 path eq '/'`    | missing `and` — this is an error, not an implicit and | `status eq 200 and path eq '/'`      |
| `age gt 18 eq true`            | comparisons do not chain                         | `(age gt 18) eq true`                     |
| `cached eq nil`                | a bool is never empty; this is always false      | `not cached`                              |

---

## 9. Error messages

Errors are reported as `error <line>:<column>: <message>` when a position is known, and
`error: <message>` otherwise. When a query you produced is rejected, the message tells you exactly
what to fix — read it and emit a corrected query.

**Syntax** (the query could not be read)

| Message | Meaning |
| --- | --- |
| `unexpected character "X"` | a character that is not part of the language, like `-`, `=`, `>`, `"` |
| `unexpected token "X"` | leftover input, usually a missing `and`/`or` |
| `expected comparison operator after expression but got "X"` | two values in a row |
| `expected an expression after 'and'` / `'or'` / `'not'` / `'eq'` | a dangling operator |
| `missing ')'` / `expected ')' but got "X"` | unbalanced parentheses |
| `unterminated string literal` | a `'` was never closed |
| `missing atom name` | a `:` not followed by a name |
| `integer literal "X" is out of range` | above 64 bit |

**Type** (the query does not match the schema)

| Message | Meaning |
| --- | --- |
| `The variable 'x' is not defined in the current schema.` | unknown field |
| `Atom ':x' is not expected.` | undeclared atom |
| `the query must evaluate to a boolean, but it evaluates to integer.` | the query is a value, not a filter |
| `Logical operator And requires boolean operands.` | `and`/`or` applied to values |
| `Cannot compare integer with string using Eq.` | mismatched types |
| `Ordered comparison Gt requires numeric or string operands, but got atom and atom.` | ordering something that cannot be ordered |
| `Operator 'reg' is only valid for strings.` | regex on a non string field |
| `Unary operator 'Not' requires a boolean operand, but got integer.` | `not` applied to a value |

**Evaluation** (valid query, problem at run time)

| Message | Meaning |
| --- | --- |
| `the variable 'x' does not exist` | the host did not provide a value for a declared field |
| `invalid regex pattern '...'` | the pattern does not compile |
| `the regex pattern '...' took too long to run` | the pattern hit the 1 second timeout |

---

## 10. Prompt template

Copy this into your system prompt and fill in the schema:

```
You translate natural language filters into Quang queries.

Quang is a boolean filter language. A query is built from:
  comparisons: <field> eq|ne|gt|lt|gte|lte <literal>, and <string field> reg '<regex>'
  combinators: not, and, or, parentheses
  literals:    integers (42), floats (0.5), strings ('text'), atoms (:name), true, false, nil
Precedence, tightest first: parentheses, comparison, not, and, or.
Types are strict: only compare a field with a literal of its own type (integers and floats mix).
'nil' means empty (null or empty string) and only works with eq and ne.
A boolean field is a filter by itself: write "active", not "active eq true".
Negative numbers, arithmetic, in/between/like, =, !=, >, <, && and || do not exist.

Schema:
<paste the field list and the allowed atoms here>

Answer with the query only, on a single line, with no quotes and no explanation.
If the request cannot be expressed with the fields above, answer: UNSUPPORTED
If the request has no filter, answer with an empty line.
```

## 11. Validating what the model produced

Never run a generated query without parsing it first — and when it fails, the error message is a
good repair prompt for a second attempt.

```csharp
using Quang;

static (bool ok, string? error) Validate(string query)
{
    try
    {
        new Quang.Quang(query)
            .Init()
            .SyntaxExpectAtom(":get")
            .SyntaxExpectSymbol("status", new ExpressionValueTypeInfo<IntegerExpression>())
            .SyntaxExpectSymbol("path", new ExpressionValueTypeInfo<StringExpression>())
            .SyntaxExpectSymbol("method", new ExpressionValueTypeInfo<AtomExpression>())
            .Evaluator();

        return (true, null);
    }
    catch (QuangException ex)
    {
        return (false, ex.Message);
    }
}
```

`QuangException` is the base of `QuangSyntaxException`, `QuangTypeException` and
`QuangEvaluationException`, and exposes `Line` and `Column` when the error has a position. A single
retry that feeds `ex.Message` back to the model fixes almost every invalid query.

---

## 12. Running the query

There are two backends. Both take the same query and the same schema.

### 12.1 Evaluator — one record at a time

Use it for in memory data: files, streams, API responses, anything you already have in a list.
Build the evaluator **once**, then change the variables for each record.

```csharp
using Quang;

var quang = new Quang.Quang(query)
    .Init()
    .SyntaxExpectAtom(":get")
    .SyntaxExpectAtom(":post")
    .SyntaxExpectSymbol("status", new ExpressionValueTypeInfo<IntegerExpression>())
    .SyntaxExpectSymbol("latency", new ExpressionValueTypeInfo<FloatExpression>())
    .SyntaxExpectSymbol("path", new ExpressionValueTypeInfo<StringExpression>())
    .SyntaxExpectSymbol("method", new ExpressionValueTypeInfo<AtomExpression>())
    .SyntaxExpectSymbol("cached", new ExpressionValueTypeInfo<BoolExpression>());

var evaluator = quang.Evaluator();   // type checks the query once

foreach (var row in rows)
{
    evaluator
        .AddIntegerVar("status", row.Status)
        .AddFloatVar("latency", row.Latency)
        .AddStringVar("path", row.Path)          // null becomes nil
        .AddAtomVar("method", $":{row.Method.ToLowerInvariant()}")  // must match a declared atom
        .AddBoolVar("cached", row.Cached);

    if (evaluator.Evaluate()) Console.WriteLine(row);
}
```

Every declared field must get a value before `Evaluate()`, otherwise it throws
`the variable 'x' does not exist`. Use `AddNilVar(name)` — or pass `null` to `AddStringVar`,
`AddIntegerVar`, `AddFloatVar` and `AddBoolVar` — for empty values.

### 12.2 LinqInterpreter — translate to a predicate

Use it when the data lives behind LINQ: an `IQueryable`, EF Core, or just a `List<T>` you want to
filter in one shot. `Translate` returns an `Expression<Func<T, bool>>`.

```csharp
using Quang;
using Quang.Interpreters;

public class LogRow
{
    public int Status { get; set; }
    public double Latency { get; set; }
    public string? Path { get; set; }
    public HttpMethod Method { get; set; }   // an enum
    public bool Cached { get; set; }
}

var predicate = new LinqInterpreter<LogRow>().Translate(quang);

var matches = db.Logs.Where(predicate).ToList();   // EF Core, translated to SQL
var inMemory = rows.Where(predicate.Compile()).ToList();
```

**Constructor options**

```csharp
new LinqInterpreter<LogRow>(
    symbolsMapping: new() { { "codigo", "Status" } },  // query field  -> property name
    atomsMapping:   new() { { ":get", "GET" } },       // atom         -> stored value / enum name
    regStrategy:    RegStrategy.Contains);             // how 'reg' is translated
```

| Option           | Default             | What it does                                                                 |
| ---------------- | ------------------- | ---------------------------------------------------------------------------- |
| `symbolsMapping` | none                | renames a query field to a property of `T`. Unmapped names are matched against public properties, case insensitively. |
| `atomsMapping`   | none                | maps an atom to the value actually stored (`":get"` → `"GET"`, or an enum member name). |
| `regStrategy`    | `RegStrategy.Regex` | `Regex` matches the Evaluator exactly. `Contains` emits `string.Contains`, which EF Core turns into SQL `LIKE`. |

**Choosing the reg strategy.** `Regex.IsMatch` cannot be translated to SQL, so a database provider
will fail or fall back to client evaluation. If the predicate goes to a database, use
`RegStrategy.Contains` — and then **tell the model in the schema block that `reg` is a plain
substring match**, so it emits `path reg 'api'` instead of `path reg '^/api/\d+$'`. Add a line like:

```
Note: 'reg' is a case sensitive substring match here, not a regular expression.
Do not use ^, $, \d or any other regex metacharacter.
```

**What the translator supports**

- comparisons where one side is a field: `status gte 400`, and also `400 lte status`
- comparisons between two fields: `size gt latency` (numeric types are widened to the largest of
  the two, so `int` against `double` works)
- `and`, `or`, `not`, parentheses, and a nested expression compared as a boolean
- boolean fields used directly: `cached`, `not cached` (works with `bool` and `bool?`)
- literals are converted to the property type, so an integer in the query works against `int`,
  `long`, `short`, `decimal`, `int?`, and an atom works against a `string` or an `enum`
- `field eq nil` becomes `string.IsNullOrEmpty(field)` for strings and a null check for reference
  and nullable types. On a non nullable value type it is constant `false`, since it can never be empty
- `field reg '...'` is null safe: it becomes `field != null && <match>`, so a null column is filtered
  out instead of throwing, and EF Core still translates it (`IS NOT NULL AND LIKE` with
  `RegStrategy.Contains`). Both sides of `reg` may be a field or a literal
- ordering strings (`path gt '/b'`) through `string.CompareOrdinal`, which matches the Evaluator.
  A database provider usually cannot translate it, so prefer comparing strings with `eq`, `ne` and
  `reg` when the predicate goes to EF Core

**What it does not support** (raises `QuangEvaluationException` at `Translate` time)

- a comparison with no field at all: `200 eq 200`
- `reg` on a property that is not a string
- a field that has no matching public property on `T`

Because `Translate` runs the type checker and then builds the tree, wrapping it in a `try/catch` of
`QuangException` validates the generated query end to end, exactly like section 11.

See [README.md](./README.md) for the rest of the host side API.
