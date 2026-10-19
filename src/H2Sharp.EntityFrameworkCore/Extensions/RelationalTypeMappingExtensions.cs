using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore.Storage;

internal static class RelationalTypeMappingExtensions
{
    internal static string GenerateEmbeddedSqlLiteral(this RelationalTypeMapping mapping, object? value)
        => mapping is H2TypeMapping npgsqlTypeMapping
            ? npgsqlTypeMapping.GenerateEmbeddedSqlLiteral(value)
            : mapping.GenerateSqlLiteral(value);
}
