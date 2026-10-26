using System.Data;
using Pure.Primitives.Bool;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Column;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;
using Pure.RelationalSchema.ColumnType;
using Pure.RelationalSchema.HashCodes;
using Pure.RelationalSchema.Storage.PostgreSQL.Creation;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Destruction.Tests;

using Column = Column.Column;
using ForeignKey = ForeignKey.ForeignKey;
using Index = Index.Index;
using Schema = Schema.Schema;
using Table = Table.Table;

public sealed record PostgreSqlDroppedSchemaTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PostgreSqlDroppedSchemaTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void DropsSchemaCreatedByCreation()
    {
        ISchema schema = CreatedWithRows("DropsSchemaCreatedByCreation");
        Assert.True(_fixture.SchemaExists(schema));

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void DropsTablesCreatedByCreation()
    {
        ISchema schema = CreatedWithRows("DropsTablesCreatedByCreation");
        Assert.Equal(2, _fixture.TableCount(schema));

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.Equal(0, _fixture.TableCount(schema));
    }

    [Fact]
    public void DropsIndexesCreatedByCreation()
    {
        ISchema schema = CreatedWithRows("DropsIndexesCreatedByCreation");
        Assert.Equal(2, _fixture.IndexCount(schema));

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.Equal(0, _fixture.IndexCount(schema));
    }

    [Fact]
    public void DropsForeignKeysCreatedByCreation()
    {
        ISchema schema = CreatedWithRows("DropsForeignKeysCreatedByCreation");
        Assert.Equal(1, _fixture.ForeignKeyCount(schema));

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.Equal(0, _fixture.ForeignKeyCount(schema));
    }

    [Fact]
    public void DropsStoredRows()
    {
        ISchema schema = CreatedWithRows("DropsStoredRows");

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;
        _ = new PostgreSqlCreatedSchema(schema, _fixture.Connection).Name;

        Assert.Equal(
            [0, 0],
            schema.Tables.Select(x => _fixture.RowCount(schema, x))
        );
    }

    [Fact]
    public void DropsSchemaWithoutTables()
    {
        ISchema schema = new Schema(new String("DropsSchemaWithoutTables"), [], []);
        _ = new PostgreSqlCreatedSchema(schema, _fixture.Connection).Name;
        Assert.True(_fixture.SchemaExists(schema));

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void LeavesOtherSchemasIntact()
    {
        ISchema dropped = CreatedWithRows("LeavesOtherSchemasIntactDropped");
        ISchema kept = CreatedWithRows("LeavesOtherSchemasIntactKept");

        _ = new PostgreSqlDroppedSchema(dropped, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(dropped));
        Assert.True(_fixture.SchemaExists(kept));
        Assert.Equal(2, _fixture.TableCount(kept));
        Assert.Equal(2, _fixture.IndexCount(kept));
        Assert.Equal(1, _fixture.ForeignKeyCount(kept));
        Assert.Equal([1, 1], kept.Tables.Select(x => _fixture.RowCount(kept, x)));
    }

    [Fact]
    public void IsNoOpWhenStorageDoesNotExist()
    {
        ISchema schema = Model("IsNoOpWhenStorageDoesNotExist");

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void IsNoOpWhenStorageWasAlreadyDropped()
    {
        ISchema schema = CreatedWithRows("IsNoOpWhenStorageWasAlreadyDropped");
        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.False(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void DoesNotDropBeforeMemberIsAccessed()
    {
        ISchema schema = CreatedWithRows("DoesNotDropBeforeMemberIsAccessed");

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection);

        Assert.True(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void DropsOnTablesAccess()
    {
        ISchema schema = CreatedWithRows("DropsOnTablesAccess");

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Tables;

        Assert.False(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void DropsOnForeignKeysAccess()
    {
        ISchema schema = CreatedWithRows("DropsOnForeignKeysAccess");

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).ForeignKeys;

        Assert.False(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void DropsOnlyOnce()
    {
        ISchema schema = CreatedWithRows("DropsOnlyOnce");
        ISchema dropped = new PostgreSqlDroppedSchema(schema, _fixture.Connection);
        _ = dropped.Name;

        _ = new PostgreSqlCreatedSchema(schema, _fixture.Connection).Name;
        _ = dropped.Name;
        _ = dropped.Tables;
        _ = dropped.ForeignKeys;

        Assert.True(_fixture.SchemaExists(schema));
    }

    [Fact]
    public void LeavesConnectionOpen()
    {
        ISchema schema = CreatedWithRows("LeavesConnectionOpen");

        _ = new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name;

        Assert.Equal(ConnectionState.Open, _fixture.Connection.State);
    }

    [Fact]
    public void ReturnsNameOfWrappedSchema()
    {
        ISchema schema = Model("ReturnsNameOfWrappedSchema");

        Assert.Equal(
            schema.Name.TextValue,
            new PostgreSqlDroppedSchema(schema, _fixture.Connection).Name.TextValue
        );
    }

    [Fact]
    public void ReturnsTablesOfWrappedSchema()
    {
        ISchema schema = Model("ReturnsTablesOfWrappedSchema");

        Assert.Equal(
            schema.Tables.Select(x => new HexString(new TableHash(x)).TextValue),
            new PostgreSqlDroppedSchema(schema, _fixture.Connection).Tables.Select(x =>
                new HexString(new TableHash(x)).TextValue
            )
        );
    }

    [Fact]
    public void ReturnsForeignKeysOfWrappedSchema()
    {
        ISchema schema = Model("ReturnsForeignKeysOfWrappedSchema");

        Assert.Equal(
            schema.ForeignKeys.Select(x =>
                new HexString(new ForeignKeyHash(x)).TextValue
            ),
            new PostgreSqlDroppedSchema(schema, _fixture.Connection).ForeignKeys.Select(
                x => new HexString(new ForeignKeyHash(x)).TextValue
            )
        );
    }

    [Fact]
    public void HashesLikeWrappedSchema()
    {
        ISchema schema = Model("HashesLikeWrappedSchema");

        Assert.Equal(
            new HexString(new SchemaHash(schema)).TextValue,
            new HexString(
                new SchemaHash(new PostgreSqlDroppedSchema(schema, _fixture.Connection))
            ).TextValue
        );
    }

    private ISchema CreatedWithRows(string name)
    {
        ISchema schema = new PostgreSqlCreatedSchema(Model(name), _fixture.Connection);
        _fixture.Insert(schema, schema.Tables.First(), "1", "Parent");
        _fixture.Insert(schema, schema.Tables.Last(), "1", "Child");
        return schema;
    }

    private static ISchema Model(string name)
    {
        IColumn id = new Column(new String("Id"), new IntColumnType());
        IColumn parentId = new Column(new String("ParentId"), new IntColumnType());
        IColumn title = new Column(new String("Title"), new StringColumnType());

        ITable parents = new Table(
            new String("Parents"),
            [id, title],
            [new Index(new True(), [id])]
        );

        ITable children = new Table(
            new String("Children"),
            [parentId, title],
            [new Index(new False(), [parentId])]
        );

        return new Schema(
            new String(name),
            [parents, children],
            [new ForeignKey(children, [parentId], parents, [id])]
        );
    }
}
