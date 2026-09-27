# LegacySqlParser

A C# console prototype that inspects SQL string literals in C# source files using Roslyn and SQL Server ScriptDom.

The current analysis finds `SELECT` statements and reports named table references without a `NOLOCK` hint. This reflects the prototype's original inspection rule; it is not a recommendation to add `NOLOCK` to application queries.

## Current behavior

- Recursively scans `.cs` files in a configured directory.
- Collects string literals containing `SELECT`, `UPDATE`, or `INSERT`.
- Combines literal fragments from string concatenation expressions.
- Parses SQL with `TSql130Parser` and prints parse errors or matching findings with source locations.

Only the first parsed SQL batch is inspected. Dynamic expressions and interpolated SQL are not fully reconstructed.

## Running the prototype

The project currently targets `net7.0`.

1. Open `ConsoleApp1/Program.cs` and replace the `PATH` value with a local source directory.
2. With a compatible .NET SDK/runtime installed, run:

```sh
dotnet restore ConsoleApp1.sln
dotnet run --project ConsoleApp1/ConsoleApp1.csproj
```

The tool reads source files and prints findings; it does not execute SQL or rewrite the scanned files. Output can contain full query text and local paths, so inspect it before sharing.

## Development priorities

- Replace the hard-coded path with a command-line argument.
- Add regression tests for multiple batches, concatenated strings, and unsupported SQL.
- Review the target framework and make inspection rules configurable.

There is currently no automated test suite, CI workflow, or project license declared in this repository.
