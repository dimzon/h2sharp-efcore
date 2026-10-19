using Microsoft.CodeAnalysis;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class NpgsqlPrecompiledQueryTestHelpers : PrecompiledQueryTestHelpers
{
    public static NpgsqlPrecompiledQueryTestHelpers Instance = new();

    protected override IEnumerable<MetadataReference> BuildProviderMetadataReferences()
    {
        yield return MetadataReference.CreateFromFile(typeof(NpgsqlOptionsExtension).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(NpgsqlConnection).Assembly.Location);
        yield return MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location);
    }
}
