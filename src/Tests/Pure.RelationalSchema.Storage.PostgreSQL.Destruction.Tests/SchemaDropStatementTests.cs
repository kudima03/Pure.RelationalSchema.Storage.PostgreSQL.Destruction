using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.PostgreSQL.Destruction.Tests.Fakes;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Destruction.Tests;

using Schema = Schema.Schema;

public sealed record SchemaDropStatementTests
{
    private static readonly string SchemaName = new string('s', 64);

    [Fact]
    public void ProducesDropSchemaIfExistsCascade()
    {
        SchemaDropStatement statement = new SchemaDropStatement(new String(SchemaName));

        Assert.Equal(
            $"DROP SCHEMA IF EXISTS \"{new string('s', 63)}\" CASCADE;",
            statement.TextValue
        );
    }

    [Fact]
    public void TrimsSchemaNameToPostgreSqlLimit()
    {
        SchemaDropStatement statement = new SchemaDropStatement(
            new String(new string('x', 100))
        );

        Assert.Equal(
            $"DROP SCHEMA IF EXISTS \"{new string('x', 63)}\" CASCADE;",
            statement.TextValue
        );
    }

    [Fact]
    public void UsesHashOfSchema()
    {
        ISchema schema = new Schema(new String("Test"), [], []);

        SchemaDropStatement statement = new SchemaDropStatement(schema);

        Assert.Equal(
            "DROP SCHEMA IF EXISTS \""
                + new HexString(new SchemaHash(schema)).TextValue[..63]
                + "\" CASCADE;",
            statement.TextValue
        );
    }

    [Fact]
    public void EnumeratesCharactersOfText()
    {
        SchemaDropStatement statement = new SchemaDropStatement(new String(SchemaName));

        Assert.Equal(statement.TextValue, Text(statement));
    }

    [Fact]
    public void EnumeratesCharactersOfTextNonGenerically()
    {
        IEnumerable statement = new SchemaDropStatement(new String(SchemaName));

        Assert.Equal(
            ((SchemaDropStatement)statement).TextValue,
            Text(new NonGenericItems(statement).Cast<IChar>())
        );
    }

    [Fact]
    public void TrimmedHashEnumeratesCharactersOfText()
    {
        IEnumerable trimmed = new TrimmedHash(new String(new string('x', 70)));

        Assert.Equal(
            new string('x', 63),
            Text(new NonGenericItems(trimmed).Cast<IChar>())
        );
    }

    [Fact]
    public void TrimmedHashEnumeratesCharactersOfTextGenerically()
    {
        TrimmedHash trimmed = new TrimmedHash(new String(new string('y', 63)));

        Assert.Equal(new string('y', 63), Text(trimmed));
    }

    private static string Text(IEnumerable<IChar> chars)
    {
        return new string([.. chars.Select(x => x.CharValue)]);
    }
}
