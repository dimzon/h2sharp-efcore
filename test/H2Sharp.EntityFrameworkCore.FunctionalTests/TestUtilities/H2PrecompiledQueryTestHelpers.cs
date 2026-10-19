using Microsoft.CodeAnalysis;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class H2PrecompiledQueryTestHelpers : PrecompiledQueryTestHelpers
{
    public static H2PrecompiledQueryTestHelpers Instance = new();

    protected override IEnumerable<MetadataReference> BuildProviderMetadataReferences()
    {
        yield return MetadataReference.CreateFromFile(typeof(H2OptionsExtension).Assembly.Location);
        yield return MetadataReference.CreateFromFile(typeof(H2Connection).Assembly.Location);
        yield return MetadataReference.CreateFromFile(Assembly.GetExecutingAssembly().Location);
    }
}
