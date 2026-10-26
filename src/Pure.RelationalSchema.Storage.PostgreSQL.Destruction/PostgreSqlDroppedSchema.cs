using System.Data;
using Pure.Primitives.Abstractions.Bool;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.Bool;
using Pure.RelationalSchema.Abstractions.ForeignKey;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.Abstractions.Table;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Destruction;

public sealed record PostgreSqlDroppedSchema : ISchema
{
    private readonly ISchema _schema;

    private readonly Lazy<IBool> _dropped;

    public PostgreSqlDroppedSchema(ISchema schema, IDbConnection connection)
    {
        _schema = schema;
        _dropped = new Lazy<IBool>(() =>
        {
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = new SchemaDropStatement(_schema).TextValue;
            _ = command.ExecuteNonQuery();
            return new True();
        });
    }

    public IString Name =>
        _dropped.Value.BoolValue ? _schema.Name : throw new ArgumentException();

    public IEnumerable<ITable> Tables =>
        _dropped.Value.BoolValue ? _schema.Tables : throw new ArgumentException();

    public IEnumerable<IForeignKey> ForeignKeys =>
        _dropped.Value.BoolValue ? _schema.ForeignKeys : throw new ArgumentException();
}
