using System.ComponentModel.DataAnnotations;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

/// <summary>
///     Provides unit tests for the pg_trgm module operator and function translations.
/// </summary>
/// <remarks>
///     See: https://www.postgresql.org/docs/current/pgtrgm.html
/// </remarks>
public class TrigramsQueryH2Test : IClassFixture<TrigramsQueryH2Test.TrigramsQueryH2Fixture>
{
    private TrigramsQueryH2Fixture Fixture { get; }

    // ReSharper disable once UnusedParameter.Local
    public TrigramsQueryH2Test(TrigramsQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
    {
        Fixture = fixture;
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    #region FunctionTests

















    #endregion

    #region Fixtures

    /// <summary>
    ///     Represents a fixture suitable for testing trigrams operators.
    /// </summary>
    public class TrigramsQueryH2Fixture : SharedStoreFixtureBase<TrigramsContext>
    {
        protected override string StoreName
            => "TrigramsQueryTest";

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override Task SeedAsync(TrigramsContext context)
            => TrigramsContext.SeedAsync(context);
    }

    /// <summary>
    ///     Represents an entity suitable for testing trigrams operators.
    /// </summary>
    public class TrigramsTestEntity
    {
        // ReSharper disable once UnusedMember.Global
        /// <summary>
        ///     The primary key.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        ///     Some text.
        /// </summary>
        public string Text { get; set; }
    }

    /// <summary>
    ///     Represents a database suitable for testing trigrams operators.
    /// </summary>
    public class TrigramsContext : PoolableDbContext
    {
        /// <summary>
        ///     Represents a set of entities with <see cref="System.String" /> properties.
        /// </summary>
        public DbSet<TrigramsTestEntity> TrigramsTestEntities { get; set; }

        /// <summary>
        ///     Initializes a <see cref="TrigramsContext" />.
        /// </summary>
        /// <param name="options">
        ///     The options to be used for configuration.
        /// </param>
        public TrigramsContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("pg_trgm");

            base.OnModelCreating(modelBuilder);
        }

        public static async Task SeedAsync(TrigramsContext context)
        {
            for (var i = 1; i <= 9; i++)
            {
                var text = "Some text " + i;
                context.TrigramsTestEntities.Add(
                    new TrigramsTestEntity { Id = i, Text = text });
            }

            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Helpers

    protected TrigramsContext CreateContext()
        => Fixture.CreateContext();

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    /// <summary>
    ///     Asserts that the SQL fragment appears in the logs.
    /// </summary>
    /// <param name="sql">The SQL statement or fragment to search for in the logs.</param>
    private void AssertContainsSql(string sql)
        => Assert.Contains(sql, Fixture.TestSqlLoggerFactory.Sql);

    #endregion
}
