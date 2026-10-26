using Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

// The engine's assembly has a public H2Sharp.Driver, and this file lives in the H2Sharp.EntityFrameworkCore
// namespace, so an unqualified Driver binds to the engine's class rather than to the test model's one:
// that adds a second entity type named Driver, with no key and only a shadow Version, and the model fails
// validation with "The entity type 'Driver' requires a primary key to be defined". An alias is used
// instead of a fully qualified name so the intent is visible. See docs/BACKLOG.md, item 4.
using F1Driver = Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel.Driver;

namespace H2Sharp.EntityFrameworkCore;

public class F1BytesH2Fixture : F1H2FixtureBase<byte[]>
{
    protected override void BuildModelExternal(ModelBuilder modelBuilder)
    {
        base.BuildModelExternal(modelBuilder);

        modelBuilder.Entity<Chassis>().Property<byte[]>("Version").HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
        modelBuilder.Entity<F1Driver>().Property<byte[]>("Version").HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
        modelBuilder.Entity<Team>().Property<byte[]>("Version").HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
        modelBuilder.Entity<Sponsor>().Property<byte[]>("Version").HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
        modelBuilder.Entity<TitleSponsor>()
            .OwnsOne(
                s => s.Details, eb =>
                {
                    eb.Property<byte[]>("Version").IsRowVersion().HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
                });
    }

    private class BytesToUIntConverter() : ValueConverter<byte[], uint>(
        bytes => BitConverter.ToUInt32(bytes),
        num => BitConverter.GetBytes(num),
        mappingHints: null);
}

public class F1H2Fixture : F1H2FixtureBase<uint>
{
    protected override void BuildModelExternal(ModelBuilder modelBuilder)
    {
        base.BuildModelExternal(modelBuilder);

        // TODO: This is a hack to work around, remove in 8.0 after https://github.com/dotnet/efcore/pull/29401
        modelBuilder.Entity<Chassis>().Property<uint>("Version").HasConversion((ValueConverter)null);
        modelBuilder.Entity<F1Driver>().Property<uint>("Version").HasConversion((ValueConverter)null);
        modelBuilder.Entity<Team>().Property<uint>("Version").HasConversion((ValueConverter)null);
        modelBuilder.Entity<Sponsor>().Property<uint>("Version").HasConversion((ValueConverter)null);
        modelBuilder.Entity<TitleSponsor>()
            .OwnsOne(
                s => s.Details, eb =>
                {
                    eb.Property<uint>("Version").IsRowVersion().HasConversion((ValueConverter)null);
                });
    }
}

public abstract class F1H2FixtureBase<TRowVersion> : F1RelationalFixture<TRowVersion>
{
    protected override ITestStoreFactory TestStoreFactory
        => H2TestStoreFactory.Instance;

    public override TestHelpers TestHelpers
        => H2TestHelpers.Instance;
}
