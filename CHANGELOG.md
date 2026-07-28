# Changelog

## 3.0.0

A stability release. Every item below was a real bug: queries that returned the wrong answer,
features that were documented but missing, or crashes that escaped the language error types.

### Breaking changes

- `IntegerExpression` now holds a `long` and `FloatExpression` a `double` (the docs always said
  64 bits). `AddIntegerVar` takes a `long` and `AddFloatVar` takes a `double`.
- Exceptions moved from the global namespace into `Quang`, and `QuangException` is now the base
  type of `QuangSyntaxException` (lexing/parsing), `QuangTypeException` (schema validation) and
  `QuangEvaluationException` (evaluation). They expose `Line` and `Column`, and no longer derive
  from `ApplicationException`. Evaluation errors that used to be `QuangSyntaxException` are now
  `QuangEvaluationException`.
- `not` binds looser than a comparison, so `not status eq 400` now means `not (status eq 400)`.
  Queries that already used parentheses are unaffected.
- The LINQ interpreter translates `reg` to `Regex.IsMatch` instead of `string.Contains`.
  Pass `RegStrategy.Contains` to the constructor to get the old behavior (and SQL `LIKE`).
- Queries that are not a boolean (like `42`) are now rejected by the type checker.
- Leftover tokens are a syntax error instead of being silently ignored.

### Fixed

- Number literals were parsed with the current culture: on any culture with a comma decimal
  separator, `weight lte 70.0` was silently comparing against `700`.
- An empty query threw instead of evaluating to `true` as documented.
- `nil` was documented but never implemented in the evaluator: any `name eq nil` threw.
  It now follows the documented semantics (`nil` and the empty string are empty, zero is not),
  with `AddNilVar` and null accepting overloads of `AddStringVar`/`AddIntegerVar`/`AddFloatVar`/`AddBoolVar`.
- Leftover tokens were ignored, so `status eq 200 name eq 'x'` (a missing `and`) silently
  evaluated only the first comparison, and `a eq 1 eq 2` only the first one too.
- A missing operand (`status eq 200 and`) crashed with a `NullReferenceException`.
- An integer literal larger than the type range crashed with an `OverflowException`.
- Booleans could not be compared (`active eq true` threw) and a boolean variable could not be
  used directly in a logical position (`active and status eq 200` threw), even though both
  passed the type checker.
- Comparing an integer against a float (`age lt 30.5`) passed the type checker and threw at
  evaluation time. Numbers are now promoted and compared.
- Ordered comparison of strings (`name gt 'm'`) was documented and implemented in the evaluator,
  but rejected by the type checker.
- The lexer only accepted the space character, so a query with a tab or a line break failed.
  Line and column are now tracked and reported correctly on multi line queries.
- Variables and atoms could not contain digits (`user1`, `:m2`, `p95`).
- A backslash that is not `\'` or `\\` is now kept as it is, so regex patterns like
  `identifier reg 'ML-\d+'` (from the README) lex correctly.
- `and`/`or` now short circuit, so the right side is only evaluated when it is needed.
- Regexes are compiled once per pattern and run with a timeout; an invalid pattern raises a
  `QuangEvaluationException` instead of a raw `ArgumentException`.
- The type checker used to run again for every `Evaluator()` call, which meant re-validating the
  whole tree once per row in the usual "filter a list" loop. The result is cached now.
- The LINQ interpreter: literals are converted to the property type (`long`, `decimal`, `int?`,
  enums, ...) instead of failing to build the expression tree; `nil` comparisons are supported;
  boolean fields can be used directly; the field may be on either side of the comparison
  (`18 lt Age`); unknown properties and unsupported nodes raise `QuangEvaluationException`.

### Added

- `Evaluator.AddNilVar(name)` and null accepting overloads for the other `Add*Var` methods.
- `Atom` implements `IEquatable<Atom>` and validates its format, so a value like `"m"`
  (missing the `:`) fails loudly instead of never matching.
- `SyntaxExpectSymbol` reports a duplicated symbol with a proper message.
