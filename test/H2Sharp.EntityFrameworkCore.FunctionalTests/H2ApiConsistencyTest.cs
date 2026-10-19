using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore;

public class H2ApiConsistencyTest(H2ApiConsistencyTest.H2ApiConsistencyFixture fixture)
    : ApiConsistencyTestBase<H2ApiConsistencyTest.H2ApiConsistencyFixture>(fixture)
{
    protected override void AddServices(ServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkH2();

    protected override Assembly TargetAssembly
        => typeof(H2RelationalConnection).Assembly;

    public class H2ApiConsistencyFixture : ApiConsistencyFixtureBase
    {
        public override HashSet<Type> FluentApiTypes { get; } =
        [
            typeof(H2DbContextOptionsBuilder),
            typeof(H2DbContextOptionsBuilderExtensions),
            typeof(H2MigrationBuilderExtensions),
            typeof(H2IndexBuilderExtensions),
            typeof(H2ModelBuilderExtensions),
            typeof(H2PropertyBuilderExtensions),
            typeof(H2EntityTypeBuilderExtensions),
            typeof(H2ServiceCollectionExtensions)
        ];

        public override HashSet<MethodInfo> UnmatchedMetadataMethods { get; } =
        [
            typeof(H2PropertyBuilderExtensions).GetMethod(
                nameof(H2PropertyBuilderExtensions.IsGeneratedTsVectorColumn),
                [typeof(PropertyBuilder), typeof(string), typeof(string[])])
        ];

        public override
            Dictionary<Type,
                (Type ReadonlyExtensions,
                Type MutableExtensions,
                Type ConventionExtensions,
                Type ConventionBuilderExtensions,
                Type RuntimeExtensions)> MetadataExtensionTypes { get; }
            = new()
            {
                {
                    typeof(IReadOnlyModel), (
                        typeof(H2ModelExtensions),
                        typeof(H2ModelExtensions),
                        typeof(H2ModelExtensions),
                        typeof(H2ModelBuilderExtensions),
                        null
                    )
                },
                {
                    typeof(IReadOnlyEntityType), (
                        typeof(H2EntityTypeExtensions),
                        typeof(H2EntityTypeExtensions),
                        typeof(H2EntityTypeExtensions),
                        typeof(H2EntityTypeBuilderExtensions),
                        null
                    )
                },
                {
                    typeof(IReadOnlyProperty), (
                        typeof(H2PropertyExtensions),
                        typeof(H2PropertyExtensions),
                        typeof(H2PropertyExtensions),
                        typeof(H2PropertyBuilderExtensions),
                        null
                    )
                },
                {
                    typeof(IReadOnlyIndex), (
                        typeof(H2IndexExtensions),
                        typeof(H2IndexExtensions),
                        typeof(H2IndexExtensions),
                        typeof(H2IndexBuilderExtensions),
                        null
                    )
                }
            };

        public override HashSet<MethodInfo> MetadataMethodExceptions { get; } =
        [
            typeof(H2EntityTypeExtensions).GetRuntimeMethod(
                nameof(H2EntityTypeExtensions.SetStorageParameter),
                [typeof(IConventionEntityType), typeof(string), typeof(object), typeof(bool)])
        ];
    }
}
