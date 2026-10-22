using H2Sharp.EntityFrameworkCore.ModelTests.Model;
using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     A database of the forum, empty or filled, and the context over it.
/// </summary>
/// <remarks>
///     <para>
///         The database is in memory and belongs to the test alone: the name of it is the name
///         of the test plus a unique suffix, so even equally named methods in different test
///         classes never see each other's rows.
///     </para>
///     <para>
///         The schema is made by the provider, and the rows are put in by it too, so the model
///         is written once and every test works with the same tables.
///     </para>
/// </remarks>
public sealed class ForumDatabase : IDisposable
{
    private readonly H2Connection _connection;
    private readonly DbContextOptions<ForumContext> _options;

    private ForumDatabase(H2Connection connection, DbContextOptions<ForumContext> options)
    {
        _connection = connection;
        _options = options;
    }

    /// <summary>
    ///     A database with the forum in it: the tables are made and the rows are put in.
    /// </summary>
    /// <param name="name">The name of the test: the database is named after it.</param>
    /// <returns>The database.</returns>
    public static ForumDatabase Filled(string name)
    {
        var database = Empty(name);

        using var context = database.Context();
        context.Database.EnsureCreated();
        ForumSeeder.Fill(context);

        return database;
    }

    /// <summary>
    ///     A database with the tables and without the rows.
    /// </summary>
    /// <param name="name">The name of the test: the database is named after it.</param>
    /// <returns>The database.</returns>
    public static ForumDatabase Empty(string name)
    {
        var connection = new H2Connection("jdbc:h2:mem:" + name + "_" + Guid.NewGuid().ToString("N"));
        connection.Open();

        var options = new DbContextOptionsBuilder<ForumContext>()
            .UseH2(connection)
            .Options;

        return new ForumDatabase(connection, options);
    }

    /// <summary>
    ///     A context over the database.
    /// </summary>
    /// <returns>The context.</returns>
    public ForumContext Context()
        => new(_options);

    /// <inheritdoc />
    public void Dispose()
        => _connection.Dispose();
}
