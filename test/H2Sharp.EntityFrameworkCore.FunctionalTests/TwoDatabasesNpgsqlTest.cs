using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class TwoDatabasesNpgsqlTest(NpgsqlFixture fixture) : TwoDatabasesTestBase(fixture), IClassFixture<NpgsqlFixture>
{
    protected new NpgsqlFixture Fixture
        => (NpgsqlFixture)base.Fixture;

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
        => new(Fixture.CreateOptions(NpgsqlTestStore.Create(databaseName)));

    protected override string DummyConnectionString { get; } = "Host=localhost;Database=DoesNotExist";
}
