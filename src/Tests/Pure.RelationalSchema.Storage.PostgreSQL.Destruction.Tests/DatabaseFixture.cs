using System.Data;
using Npgsql;
using Pure.HashCodes.Abstractions;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.HashCodes;
using Testcontainers.PostgreSql;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Destruction.Tests;

public sealed record DatabaseFixture : IDisposable
{
    private const int MaxIdentifierLength = 63;

    private readonly PostgreSqlContainer _postgres;

    public IDbConnection Connection { get; }

    public DatabaseFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres")
            .WithDatabase("testdb")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        _postgres.StartAsync().GetAwaiter().GetResult();

        Connection = new NpgsqlConnection(_postgres.GetConnectionString());

        Connection.Open();
    }

    public bool SchemaExists(ISchema schema)
    {
        return Count(
                $"SELECT count(*) FROM pg_catalog.pg_namespace "
                    + $"WHERE nspname = '{Name(new SchemaHash(schema))}';"
            ) == 1;
    }

    public long TableCount(ISchema schema)
    {
        return Count(
            $"SELECT count(*) FROM pg_catalog.pg_tables "
                + $"WHERE schemaname = '{Name(new SchemaHash(schema))}';"
        );
    }

    public long IndexCount(ISchema schema)
    {
        return Count(
            $"SELECT count(*) FROM pg_catalog.pg_indexes "
                + $"WHERE schemaname = '{Name(new SchemaHash(schema))}';"
        );
    }

    public long ForeignKeyCount(ISchema schema)
    {
        return Count(
            $"SELECT count(*) FROM information_schema.table_constraints "
                + $"WHERE constraint_schema = '{Name(new SchemaHash(schema))}' "
                + "AND constraint_type = 'FOREIGN KEY';"
        );
    }

    public long RowCount(ISchema schema, ITable table)
    {
        return Count($"SELECT count(*) FROM {QualifiedName(schema, table)};");
    }

    public void Insert(ISchema schema, ITable table, params string[] values)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText =
            $"INSERT INTO {QualifiedName(schema, table)} ("
            + string.Join(
                ", ",
                table.Columns.Select(x => $"\"{Name(new ColumnHash(x))}\"")
            )
            + ") VALUES ("
            + string.Join(", ", values.Select(x => $"'{x}'"))
            + ");";
        _ = command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        Connection.Dispose();
        _postgres.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static string Name(IDeterminedHash hash)
    {
        string name = new HexString(hash).TextValue;
        return name[..Math.Min(MaxIdentifierLength, name.Length)];
    }

    private static string QualifiedName(ISchema schema, ITable table)
    {
        return $"\"{Name(new SchemaHash(schema))}\".\"{Name(new TableHash(table))}\"";
    }

    private long Count(string sql)
    {
        using IDbCommand command = Connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }
}
