using System.Collections;
using Pure.Primitives.Abstractions.Char;
using Pure.Primitives.Abstractions.String;
using Pure.Primitives.String;
using Pure.Primitives.String.Operations;
using Pure.RelationalSchema.Abstractions.Schema;
using Pure.RelationalSchema.HashCodes;
using Char = Pure.Primitives.Char.Char;
using String = Pure.Primitives.String.String;

namespace Pure.RelationalSchema.Storage.PostgreSQL.Destruction;

internal sealed record SchemaDropStatement : IString
{
    private readonly IString _schemaName;

    public SchemaDropStatement(ISchema schema)
        : this(new HexString(new SchemaHash(schema)))
    { }

    public SchemaDropStatement(IString schemaName)
    {
        _schemaName = schemaName;
    }

    public string TextValue =>
        new ConcatenatedString(
            new WhitespaceJoinedString(
                new String("DROP SCHEMA IF EXISTS"),
                new WrappedString(new DoubleQuoteString(), new TrimmedHash(_schemaName)),
                new String("CASCADE")
            ),
            new SemicolonString()
        ).TextValue;

    public IEnumerator<IChar> GetEnumerator()
    {
        return TextValue.Select(x => new Char(x)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
