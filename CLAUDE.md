# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All `dotnet` commands must be run from the `./src` directory.

```bash
dotnet restore
dotnet build --no-restore -warnaserror
dotnet format --verify-no-changes             # check code style (CI enforces this)
dotnet format                                 # auto-fix code style
dotnet test --no-build --verbosity normal     # run integration tests (requires Docker for Testcontainers)
dotnet tool restore && dotnet stryker --mutation-level Complete --break-at 100
dotnet pack --configuration Release -p:Version=<version> --output .
```

CI additionally passes `-p:AssemblyVersion` (pinned to the major) and `-p:FileVersion`; see `.github/workflows/publish-nuget.yml`. Stryker leaves a mutated assembly in `bin/`, so rebuild before running `dotnet test --no-build` after it.

## Architecture

This is the **destruction (DDL)** part of the PostgreSQL storage implementation for the Pure relational schema ecosystem, the counterpart of `Pure.RelationalSchema.Storage.PostgreSQL.Creation`. It has no public abstractions of its own: it provides one public decorator that implements `ISchema`. Creating the storage, reading rows, inserting rows and deleting rows live in the sibling packages Creation, Querying, Insertion and Deletion. The design deliberately mirrors `PostgreSqlCreatedSchema` so the two DDL packages stay symmetric.

**Dependencies:** the library references only `Pure.Primitives.String.Operations` (which brings in `Pure.Primitives`) and `Pure.RelationalSchema.HashCodes` (which brings in `Pure.RelationalSchema.Abstractions` and `Pure.Primitives.Abstractions`). Do not add direct references to packages that already come transitively. It does not reference the concrete `Pure.RelationalSchema` model or any storage package. Tests additionally reference the published `Pure.RelationalSchema.Storage.PostgreSQL.Creation` package, which also brings in the `Pure.RelationalSchema` model types, so they prove the naming contract against real created storage.

**Key relationships:**

- `PostgreSqlDroppedSchema` (public) wraps an `ISchema` and an `IDbConnection`. On first access of `Name`, `Tables` or `ForeignKeys` it lazily runs one `SchemaDropStatement`, then returns the wrapped schema's members. The `throw new ArgumentException()` arms are unreachable and mirror Creation; they account for the uncovered branches. They have no message on purpose, so they produce no NoCoverage string mutants.
- `SchemaDropStatement` (internal) builds `DROP SCHEMA IF EXISTS "<schema>" CASCADE;`. `IF EXISTS` makes a missing storage a no-op, and `CASCADE` removes the tables, rows, indexes and foreign keys inside. Its `ISchema` constructor derives the name from `SchemaHash`; the `IString` constructor takes a raw name so tests can assert the exact SQL. The name is trimmed inside `TextValue`.
- The schema is the only unit worth dropping. Physical names are content hashes of the whole model, so a changed table produces a different schema name; do not add table-level drops.
- All SQL is composed as an `IString` value using `Pure.Primitives.String.Operations` combinators. Do not use string interpolation in library code.
- The caller owns the `IDbConnection`. It is shared with every other decorator using it, so no type here may dispose it (the `IDbCommand` is disposed).

**Naming convention:** the schema is named with the first 63 characters (PostgreSQL's identifier limit) of `HexString` over `SchemaHash` from `Pure.RelationalSchema.HashCodes`. This must match Creation exactly, since it is the only contract between the two packages. `TrimmedHash` is deliberately duplicated in each repo rather than shared; it expects input of at least 63 characters (a 64-character SHA-256 hex string in practice).

**Testing:** Integration tests use `Testcontainers.PostgreSql`. `PostgreSqlDroppedSchemaTests` shares one container per test class through `IClassFixture<DatabaseFixture>`, so **every test uses a distinct schema name** (usually the test's own name) to get its own hash-named schema. Storage is created with `PostgreSqlCreatedSchema` from a model with two tables (a unique and a non-unique index) and one foreign key, then seeded with `DatabaseFixture.Insert`. Effects are observed through the PostgreSQL catalog (`pg_namespace`, `pg_tables`, `pg_indexes`, `information_schema.table_constraints`), never through the decorator. `DatabaseFixture` computes physical names independently (`hex[..63]`) instead of using the internal `TrimmedHash`, so the tests check the contract rather than the implementation.

- The decorator is lazy, so touch a member (usually `_ = dropped.Name;`) to trigger the drop. To prove the rows are gone, drop and then recreate with `PostgreSqlCreatedSchema` and count the rows of the fresh tables.
- `DropsOnlyOnce` recreates the storage after the first access and checks that later accesses do not drop it again.
- `SchemaDropStatementTests` asserts the exact SQL text (mutation-testing targets) and uses 64-character names because `TrimmedHash` takes a fixed 63-character prefix.
- Non-generic enumeration is exercised through `Fakes/NonGenericItems`; `Cast<T>()` short-circuits when the source already implements `IEnumerable<T>` and would not call the non-generic `GetEnumerator`.

CI runs mutation testing via `dotnet-stryker` at `Complete` level with a 100% break threshold and requires >=99% line coverage (warning at 99%).

**CI style check:** the `Code style check by csharpier` step in `.github/workflows/build-and-test.yml` is commented out on purpose. csharpier wants `) { }` on one line for chained constructors with an empty body, while `dotnet format` (IDE0055 with `csharp_new_line_before_open_brace = all`) wants `{ }` on its own line, so both cannot pass on `SchemaDropStatement.cs`. `dotnet format` is the authoritative check.

**Multi-targeting:** net8.0, net9.0, net10.0. `IsAotCompatible` is `true` and the build must stay free of IL2026/IL3050 warnings. Do not use `AsQueryable()` in library code (annotated as trim/AOT-unsafe).

**Package validation:** `EnablePackageValidation = true`. There is no `PackageValidationBaselineVersion` until the first release; set it to the last published version after each release so breaking API changes fail the build.

**Publishing:** triggered by pushing a semver tag matching `*.*.*`. The tag name becomes the `PackageVersion`. Packages are published to both GitHub Packages and NuGet.org.

## Code Style

Enforced via `.editorconfig` and `dotnet format --verify-no-changes` in CI:

- No `var` — always use explicit types
- No expression-bodied methods or constructors — properties, indexers, accessors, and lambdas may be expression-bodied
- File-scoped namespace declarations
- Private fields prefixed with `_` (underscore camelCase)
- No non-private instance fields
- `using` directives outside the namespace, System directives sorted first
- Max line length: 90 characters
- Braces on new lines (`csharp_new_line_before_open_brace = all`)
- No implicit object creation (`new()` without type) when the type is apparent

## Commit Messages

Do not mention Claude or AI assistance in commit messages.
