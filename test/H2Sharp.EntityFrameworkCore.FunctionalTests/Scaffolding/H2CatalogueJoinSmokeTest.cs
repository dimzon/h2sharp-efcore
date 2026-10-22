using H2Sharp.EntityFrameworkCore.TestUtilities;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Scaffolding;

/// <summary>
///     A smoke test of the joins in the port: the scaffolding reads the catalogue with queries that
///     join the views of INFORMATION_SCHEMA, and the port has to read them for the scaffolding to
///     work at all.
/// </summary>
/// <remarks>
///     <para>
///         The port reads the joins since the commit "The join reads two tables as one"
///         ("49bccfc"): <c>INNER</c>, <c>JOIN</c>, <c>LEFT</c>/<c>RIGHT [OUTER]</c>, <c>CROSS</c> and
///         <c>NATURAL</c>, with the condition <c>ON</c> and the list <c>USING</c>.
///     </para>
///     <para>
///         The queries here are the queries of the scaffolding itself, unchanged, so that the test
///         fails if the port stops reading them.
///     </para>
///     <para>
///         The test keeps one connection open all the time: an in-memory database of the port dies
///         with the last connection (the gap with <c>DB_CLOSE_DELAY</c>).
///     </para>
/// </remarks>
public class H2CatalogueJoinSmokeTest : IClassFixture<H2CatalogueJoinSmokeTest.H2JoinFixture>
{
    protected H2JoinFixture Fixture { get; }

    public H2CatalogueJoinSmokeTest(H2JoinFixture fixture)
        => Fixture = fixture;

    [Fact]
    public void The_port_reads_an_inner_join()
    {
        var rows = Query(
            """
            SELECT "Blogs"."Id", "Blogs"."Title", "Tags"."Tag"
            FROM "Blogs"
            JOIN "Tags" ON "Tags"."BlogId" = "Blogs"."Id"
            ORDER BY "Blogs"."Id", "Tags"."Tag"
            """);

        Assert.Collection(
            rows,
            row => Assert.Equal(new[] { "1", "First", "h2" }, row),
            row => Assert.Equal(new[] { "1", "First", "sharp" }, row),
            row => Assert.Equal(new[] { "2", "Second", "sharp" }, row));
    }

    [Fact]
    public void The_port_reads_a_left_join()
    {
        var rows = Query(
            """
            SELECT "Blogs"."Id", "Blogs"."Title", "Tags"."Tag"
            FROM "Blogs"
            LEFT JOIN "Tags" ON "Tags"."BlogId" = "Blogs"."Id"
            ORDER BY "Blogs"."Id", "Tags"."Tag"
            """);

        Assert.Collection(
            rows,
            row => Assert.Equal(new[] { "1", "First", "h2" }, row),
            row => Assert.Equal(new[] { "1", "First", "sharp" }, row),
            row => Assert.Equal(new[] { "2", "Second", "sharp" }, row),
            row => Assert.Equal(new[] { "3", "Third", null }, row));
    }

    [Fact]
    public void The_port_reads_the_query_of_the_keys_of_the_scaffolding()
    {
        // The query of PopulateKeys of the scaffolding.
        var rows = Query(
            """
            SELECT tc.TABLE_NAME, tc.CONSTRAINT_NAME, tc.CONSTRAINT_TYPE, kcu.COLUMN_NAME
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                   ON kcu.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
                  AND kcu.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA
            WHERE tc.TABLE_SCHEMA = 'PUBLIC'
              AND tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE')
            ORDER BY tc.TABLE_NAME, tc.CONSTRAINT_NAME, kcu.ORDINAL_POSITION
            """);

        // The name of the primary key constraint is the one the port gave it, and the columns of
        // the key are the columns of the table.
        var key = Assert.Single(rows, r => r[0] == "Blogs" && r[2] == "PRIMARY KEY");
        Assert.Equal("Id", key[3]);
    }

    [Fact]
    public void The_port_reads_the_query_of_the_indexes_of_the_scaffolding()
    {
        // The query of PopulateIndexes of the scaffolding.
        var rows = Query(
            """
            SELECT i.INDEX_NAME, i.TABLE_NAME, i.INDEX_TYPE_NAME, c.COLUMN_NAME
            FROM INFORMATION_SCHEMA.INDEXES i
            JOIN INFORMATION_SCHEMA.INDEX_COLUMNS c
                 ON c.INDEX_SCHEMA = i.INDEX_SCHEMA
                AND c.INDEX_NAME = i.INDEX_NAME
            WHERE i.INDEX_SCHEMA = 'PUBLIC'
              AND i.IS_GENERATED = FALSE
            ORDER BY i.INDEX_NAME, c.ORDINAL_POSITION
            """);

        var index = Assert.Single(rows, r => r[0] == "IX_BLOGS_TITLE");
        Assert.Equal("Blogs", index[1]);
        Assert.Equal("Title", index[3]);
    }

    /// <summary>
    ///     Runs a query and reads its rows as the text of the columns.
    /// </summary>
    private List<string[]> Query(string sql)
    {
        var rows = new List<string[]>();

        using var command = Fixture.Connection.CreateCommand();
        command.CommandText = sql;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetString(i);
            }

            rows.Add(row);
        }

        return rows;
    }

    public class H2JoinFixture : SharedStoreFixtureBase<PoolableDbContext>
    {
        protected override string StoreName { get; } = nameof(H2CatalogueJoinSmokeTest);

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public new H2TestStore TestStore
            => (H2TestStore)base.TestStore;

        /// <summary>
        ///     The connection that keeps the in-memory database alive.
        /// </summary>
        public H2Connection Connection { get; private set; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            Connection = new H2Connection(TestStore.ConnectionString);
            Connection.Open();

            await ExecuteAsync("""
                CREATE TABLE "Blogs" ("Id" INT PRIMARY KEY, "Title" VARCHAR(50) NOT NULL)
                """);
            await ExecuteAsync("""
                CREATE TABLE "Tags" ("Tag" VARCHAR(50) NOT NULL, "BlogId" INT NOT NULL)
                """);
            await ExecuteAsync("""
                CREATE INDEX "IX_BLOGS_TITLE" ON "Blogs" ("Title")
                """);
            await ExecuteAsync("""
                INSERT INTO "Blogs" ("Id", "Title") VALUES (1, 'First'), (2, 'Second'), (3, 'Third')
                """);
            await ExecuteAsync("""
                INSERT INTO "Tags" ("Tag", "BlogId") VALUES ('h2', 1), ('sharp', 1), ('sharp', 2)
                """);
        }

        public override async Task DisposeAsync()
        {
            Connection?.Dispose();

            await base.DisposeAsync();
        }

        private async Task ExecuteAsync(string sql)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
    }
}
