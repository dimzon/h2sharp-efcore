using System.ComponentModel.DataAnnotations.Schema;
using H2Sharp.EntityFrameworkCore.TestUtilities;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace H2Sharp.EntityFrameworkCore.Query;

public class QueryBugsTest : IClassFixture<H2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public QueryBugsTest(H2Fixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    protected H2Fixture Fixture { get; }

    #region Bug920

    [Fact]
    public async Task Bug920()
    {
        await using var _ = await CreateDatabase920Async();
        using var context = new Bug920Context(_options);
        context.Entities.Add(new Bug920Entity { Enum = Bug920Enum.Two });
        context.SaveChanges();
    }

    private Task<H2TestStore> CreateDatabase920Async()
        => CreateTestStoreAsync(() => new Bug920Context(_options), _ => ClearLog());

    public enum Bug920Enum { One, Two }

    public class Bug920Entity
    {
        public int Id { get; set; }

        [Column(TypeName = "char(3)")]
        public Bug920Enum Enum { get; set; }
    }

    private class Bug920Context(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Bug920Entity> Entities { get; set; }
    }

    #endregion Bug920

    private DbContextOptions _options;

    private async Task<H2TestStore> CreateTestStoreAsync<TContext>(
        Func<TContext> contextCreator,
        Action<TContext> contextInitializer)
        where TContext : DbContext, IDisposable
    {
        var testStore = await H2TestStore.CreateInitializedAsync("QueryBugsTest");

        _options = Fixture.CreateOptions(testStore);

        await using var context = contextCreator();
        await context.Database.EnsureCreatedResilientlyAsync();
        contextInitializer?.Invoke(context);
        return testStore;
    }

    protected void ClearLog()
        => Fixture.TestSqlLoggerFactory.Clear();

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);
}
