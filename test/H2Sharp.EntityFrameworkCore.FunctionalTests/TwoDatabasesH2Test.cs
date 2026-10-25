using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class TwoDatabasesH2Test(H2Fixture fixture) : TwoDatabasesTestBase(fixture), IClassFixture<H2Fixture>
{
    protected new H2Fixture Fixture
        => (H2Fixture)base.Fixture;

    protected override DbContextOptionsBuilder CreateTestOptions(
        DbContextOptionsBuilder optionsBuilder,
        bool withConnectionString = false,
        bool withNullConnectionString = false)
        => withConnectionString
            ? withNullConnectionString
                ? optionsBuilder.UseH2((string)null)
                : optionsBuilder.UseH2(DummyConnectionString)
            : optionsBuilder.UseH2();

    protected override TwoDatabasesWithDataContext CreateBackingContext(string databaseName)
        => new(Fixture.CreateOptions(H2TestStore.Create(databaseName)));

    protected override string DummyConnectionString { get; } = "jdbc:h2:mem:does_not_exist";
}
