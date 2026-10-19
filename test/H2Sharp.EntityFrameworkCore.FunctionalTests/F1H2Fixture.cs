using Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class F1BytesH2Fixture : F1H2FixtureBase<byte[]>
{
    protected override void BuildModelExternal(ModelBuilder modelBuilder)
    {
        base.BuildModelExternal(modelBuilder);

        modelBuilder.Entity<Chassis>().Property<byte[]>("Version").HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
        modelBuilder.Entity<Driver>().Property<byte[]>("Version").HasConversion<BytesToUIntConverter>(new ArrayStructuralComparer<byte>());
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
        modelBuilder.Entity<Driver>().Property<uint>("Version").HasConversion((ValueConverter)null);
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
