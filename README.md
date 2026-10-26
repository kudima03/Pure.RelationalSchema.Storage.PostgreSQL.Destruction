# Pure.RelationalSchema.Storage.PostgreSQL.Destruction

PostgreSQL schema destruction for the **Pure** relational schema ecosystem — drops the physical storage created by `Pure.RelationalSchema.Storage.PostgreSQL.Creation`.

[![.NET build & test](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Destruction/actions/workflows/build-and-test.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Destruction/actions/workflows/build-and-test.yml)
[![publish NuGet](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Destruction/actions/workflows/publish-nuget.yml/badge.svg?branch=main)](https://github.com/kudima03/Pure.RelationalSchema.Storage.PostgreSQL.Destruction/actions/workflows/publish-nuget.yml)
[![NuGet](https://img.shields.io/nuget/v/Pure.RelationalSchema.Storage.PostgreSQL.Destruction)](https://www.nuget.org/packages/Pure.RelationalSchema.Storage.PostgreSQL.Destruction)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## Overview

`Pure.RelationalSchema.Storage.PostgreSQL.Destruction` removes the physical PostgreSQL storage of an `ISchema` from a live database. `PostgreSqlDroppedSchema` wraps any `ISchema` and lazily executes one statement, the first time `Name`, `Tables` or `ForeignKeys` is accessed:

```sql
DROP SCHEMA IF EXISTS "<schema>" CASCADE;
```

After that, the decorator returns the wrapped schema's members unchanged. It is the counterpart of `PostgreSqlCreatedSchema` from the Creation package and mirrors its shape. Dropping works as follows:

- `CASCADE` removes everything inside the PostgreSQL schema: tables, their rows, indexes and foreign keys.
- `IF EXISTS` makes it a no-op when the storage was never created or was already dropped.
- Nothing is executed until a member is accessed, and the statement runs at most once per decorator instance.
- Other schemas are left alone, including schemas created from a model with the same tables but a different name.

The schema is the only unit this package drops. Physical names are content hashes of the whole model (see below), so any change to a table or column produces a different schema name. Dropping individual tables of an existing storage is therefore out of scope.

The caller owns the `IDbConnection`: it must be opened before use, and this package never disposes it. The command created for the `DROP` is disposed.

## Public API

| Type | Kind | Description |
|------|------|-------------|
| `PostgreSqlDroppedSchema` | record | `ISchema` decorator that drops the schema's PostgreSQL storage lazily on first property access |

The `DROP SCHEMA` statement builder and the hashing helper are internal.

## Naming convention

Schema identifiers in PostgreSQL are not the model names. The dropped schema is named with the first 63 hexadecimal characters (PostgreSQL's identifier limit) of `SchemaHash` from `Pure.RelationalSchema.HashCodes`, exactly as `Pure.RelationalSchema.Storage.PostgreSQL.Creation` names it. This convention is the only contract between the two packages, and the tests prove it against storage created by the published Creation package.

The hash covers the schema name, its tables, their columns and indexes, and its foreign keys. To drop a storage, pass the same model that was used to create it. A model that differs in any detail resolves to another schema name, and dropping it is a no-op.

## Dependencies

- [`Pure.RelationalSchema.HashCodes`](https://github.com/kudima03/Pure.RelationalSchema.HashCodes/tree/3.3.0) — `SchemaHash`, used to derive the PostgreSQL schema name; brings in the `ISchema` abstractions
- [`Pure.Primitives.Abstractions`](https://github.com/kudima03/Pure.Primitives.Abstractions/tree/4.3.0) — base interfaces for primitive types (`IString`, `IBool`, etc.)
- [`Pure.Primitives`](https://github.com/kudima03/Pure.Primitives/tree/3.7.0) — concrete primitive type implementations
- [`Pure.Primitives.String.Operations`](https://github.com/kudima03/Pure.Primitives.String.Operations/tree/1.7.0) — string composition utilities used to build the SQL statement

## Target Frameworks

- .NET 8
- .NET 9
- .NET 10

## Installation

```
dotnet add package Pure.RelationalSchema.Storage.PostgreSQL.Destruction
```

The example below also uses `Pure.RelationalSchema` for the concrete model types and `Pure.RelationalSchema.Storage.PostgreSQL.Creation` to create the storage first; neither is a dependency of this package.

## Usage

```csharp
using System.Data;
using Npgsql;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;
using Pure.RelationalSchema.Storage.PostgreSQL.Destruction;
using Column = Pure.RelationalSchema.Column.Column;
using Schema = Pure.RelationalSchema.Schema.Schema;
using String = Pure.Primitives.String.String;
using Table = Pure.RelationalSchema.Table.Table;

IDbConnection connection = new NpgsqlConnection(connectionString);
connection.Open();

IColumn idColumn = new Column(new String("Id"), new IntColumnType());
IColumn nameColumn = new Column(new String("Name"), new StringColumnType());
ITable usersTable = new Table(new String("Users"), [idColumn, nameColumn], []);
ISchema schema = new Schema(new String("Shop"), [usersTable], []);

// Both decorators are lazy: the SQL runs on first access to a member.
_ = new PostgreSqlCreatedSchema(schema, connection).Name;

// Drops the schema, its tables, rows, indexes and foreign keys.
ISchema dropped = new PostgreSqlDroppedSchema(schema, connection);
_ = dropped.Name;

// Running it again is safe: the storage no longer exists, so nothing happens.
_ = new PostgreSqlDroppedSchema(schema, connection).Name;
```

## Known limitations

- **`CASCADE` reaches beyond the created objects.** Everything inside the schema is dropped, including objects that were added to it by hand (views, functions, sequences). Objects in other schemas that depend on it, such as a view or a foreign key that references one of its tables, are dropped too.
- **It cannot be undone.** The rows are gone once the statement runs. There is no confirmation and no soft delete.
- **The model must match exactly.** Storage is found only through the hash of the model it was created from. A storage whose model has been lost or changed cannot be addressed by this package.
- **It takes exclusive locks.** `DROP SCHEMA` waits for, and then blocks, every other session that uses the schema's tables until the statement finishes or the caller's transaction ends.
- **Members describe the model, not the storage.** After the drop, `Tables` and `ForeignKeys` still return the wrapped model's values, although the physical tables no longer exist.

## Related packages

- `Pure.RelationalSchema.Storage.PostgreSQL.Creation` — creates the storage this package drops
- `Pure.RelationalSchema.Storage.PostgreSQL.Querying` — reads rows from that storage
- `Pure.RelationalSchema.Storage.PostgreSQL.Insertion` — inserts rows into that storage
- `Pure.RelationalSchema.Storage.PostgreSQL.Deletion` — deletes rows from that storage while keeping the tables
