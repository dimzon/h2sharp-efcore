using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Migrations;

public class H2HistoryRepositoryTest
{
    [ConditionalFact]
    public void GetCreateScript_works()
    {
        var sql = CreateHistoryRepository().GetCreateScript();

        Assert.Equal(
            """
CREATE TABLE "__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL,
    "ProductVersion" varchar(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetCreateScript_works_with_schema()
    {
        var sql = CreateHistoryRepository("my").GetCreateScript();

        Assert.Equal(
            """
CREATE SCHEMA IF NOT EXISTS "my";
CREATE TABLE "my"."__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL,
    "ProductVersion" varchar(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetCreateIfNotExistsScript_works()
    {
        var sql = CreateHistoryRepository().GetCreateIfNotExistsScript();

        Assert.Equal(
            """
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL,
    "ProductVersion" varchar(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetCreateIfNotExistsScript_works_with_schema()
    {
        var sql = CreateHistoryRepository("my").GetCreateIfNotExistsScript();

        Assert.Equal(
            """
CREATE SCHEMA IF NOT EXISTS "my";
CREATE TABLE IF NOT EXISTS "my"."__EFMigrationsHistory" (
    "MigrationId" varchar(150) NOT NULL,
    "ProductVersion" varchar(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetDeleteScript_works()
    {
        var sql = CreateHistoryRepository().GetDeleteScript("Migration1");

        Assert.Equal(
            """
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" = 'Migration1';

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetInsertScript_works()
    {
        var sql = CreateHistoryRepository().GetInsertScript(
            new HistoryRow("Migration1", "7.0.0"));

        // The row goes in only if it is not there yet, and H2 2.5 says that with the
        // condition of the insert: there is no DO block in H2.
        Assert.Equal(
            """
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
SELECT 'Migration1', '7.0.0'
WHERE NOT EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = 'Migration1');

""", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetBeginIfNotExistsScript_works()
    {
        var sql = CreateHistoryRepository().GetBeginIfNotExistsScript("Migration1");

        Assert.Equal("", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetBeginIfExistsScript_works()
    {
        var sql = CreateHistoryRepository().GetBeginIfExistsScript("Migration1");

        Assert.Equal("", sql, ignoreLineEndingDifferences: true);
    }

    [ConditionalFact]
    public void GetEndIfScript_works()
    {
        var sql = CreateHistoryRepository().GetEndIfScript();

        Assert.Equal("", sql, ignoreLineEndingDifferences: true);
    }

    private static IHistoryRepository CreateHistoryRepository(string schema = null)
        => new TestDbContext(
                new DbContextOptionsBuilder()
                    .UseInternalServiceProvider(H2TestHelpers.Instance.CreateServiceProvider())
                    .UseH2(
                        new H2Connection("Host=localhost;Database=DummyDatabase"),
                        b => b.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
                    .Options)
            .GetService<IHistoryRepository>();

    private class TestDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Blog> Blogs { get; set; }

        [DbFunction("TableFunction")]
        public IQueryable<TableFunction> TableFunction()
            => FromExpression(() => TableFunction());

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }

    private class Blog
    {
        public int Id { get; set; }
    }

    private class TableFunction
    {
        public int Id { get; set; }
        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}
