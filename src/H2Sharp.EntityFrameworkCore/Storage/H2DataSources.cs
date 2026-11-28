using System.Collections.Concurrent;
using H2Sharp.Data;
using H2Sharp.Engine;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.Storage;

/// <summary>
///     The data sources of the provider: one data source per database.
/// </summary>
/// <remarks>
///     <para>
///         An H2 database is owned by an <see cref="H2DataSource" />: the data source creates it, keeps it
///         alive and hands out connections to it. Two connections that come from two data sources are two
///         databases, even when they carry the same name.
///     </para>
///     <para>
///         One database has one data source, and this is where that is kept: a connection string names a
///         database, and every context with that connection string takes its connections from the one data
///         source that owns it. A file database is a case where this matters twice over: two owners of one
///         file collide on the file lock.
///     </para>
///     <para>
///         The key is the identity of the database and not the spelling of the connection string, so two
///         spellings of one database -- the same file under two paths, the same memory key with and without
///         the mode -- lead to one data source and not to two.
///     </para>
///     <para>
///         A caller that owns a data source of its own hands it over with
///         <c>UseH2(DbDataSource)</c>, and the provider takes the connections from it. A caller that has
///         only a connection string gets the data source of this table, and a connection to that database
///         is a connection from that data source.
///     </para>
/// </remarks>
public static class H2DataSources
{
    private static readonly ConcurrentDictionary<string, H2DataSource> Sources = new(StringComparer.Ordinal);

    /// <summary>
    ///     The data source that owns the database the connection string names.
    /// </summary>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <returns>The data source that owns the database.</returns>
    public static H2DataSource GetOrCreate(string connectionString)
        => Sources.GetOrAdd(
            Key(connectionString),
            _ => new H2DataSourceBuilder(connectionString)
                .ConfigureExtensions(H2JsonPathUdfs.Register)
                .Build());

    /// <summary>
    ///     The data source that owns the database the connection string names, if there is one.
    /// </summary>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <returns>The data source that owns the database, or null.</returns>
    public static H2DataSource? Find(string connectionString)
        => Sources.TryGetValue(Key(connectionString), out var dataSource) ? dataSource : null;

    /// <summary>
    ///     The key the database of the connection string is held under.
    /// </summary>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <returns>The key of the database.</returns>
    private static string Key(string connectionString)
    {
        ConnectionInfo info = new H2ConnectionStringBuilder(connectionString).ToConnectionInfo();

        var name = info.DataSource ?? string.Empty;
        if (info.Storage == StorageKind.File && name.Length > 0)
        {
            // The same file under two spellings is one database, and the path decides which file it
            // is: the engine opens the file relative to the current directory as well.
            name = Path.GetFullPath(name);
        }

        return string.Join('\u001f', info.Storage, name, info.Mode.Name, info.Options.Init ?? string.Empty);
    }
}
