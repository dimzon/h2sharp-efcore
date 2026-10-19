using System.Data.Common;
using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Provides extension methods on <see cref="DbContextOptionsBuilder" /> and <see cref="DbContextOptionsBuilder{T}" />
///     used to configure a <see cref="DbContext" /> to context to a PostgreSQL database with H2.
/// </summary>
public static class H2DbContextOptionsBuilderExtensions
{
    /// <summary>
    ///     <para>
    ///         Configures the context to connect to a PostgreSQL server with H2, but without initially setting any
    ///         <see cref="DbConnection" /> or connection string.
    ///     </para>
    ///     <para>
    ///         The connection or connection string must be set before the <see cref="DbContext" /> is used to connect
    ///         to a database. Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection" />.
    ///         Set a connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString" />.
    ///     </para>
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseH2(
        this DbContextOptionsBuilder optionsBuilder,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));

        ConfigureWarnings(optionsBuilder);

        h2OptionsAction?.Invoke(new H2DbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">A builder for setting options on the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>
    ///     The options builder so that further configuration can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseH2(
        this DbContextOptionsBuilder optionsBuilder,
        string? connectionString,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));

        var extension = (H2OptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        ConfigureWarnings(optionsBuilder);

        h2OptionsAction?.Invoke(new H2DbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed. The caller owns the connection and is
    ///     responsible for its disposal.
    /// </param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder UseH2(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
        => UseH2(optionsBuilder, connection, contextOwnsConnection: false, h2OptionsAction);

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">A builder for setting options on the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed.
    /// </param>
    /// <param name="contextOwnsConnection">
    ///     If <see langword="true" />, then EF will take ownership of the connection and will
    ///     dispose it in the same way it would dispose a connection created by EF. If <see langword="false" />, then the caller still
    ///     owns the connection and is responsible for its disposal.
    /// </param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>
    ///     The options builder so that further configuration can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseH2(
        this DbContextOptionsBuilder optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));
        Check.NotNull(connection, nameof(connection));

        var extension = (H2OptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnection(connection, contextOwnsConnection);

        ConfigureWarnings(optionsBuilder);

        h2OptionsAction?.Invoke(new H2DbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">A builder for setting options on the context.</param>
    /// <param name="dataSource">A <see cref="DbDataSource" /> which will be used to get database connections.</param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>
    ///     The options builder so that further configuration can be chained.
    /// </returns>
    public static DbContextOptionsBuilder UseH2(
        this DbContextOptionsBuilder optionsBuilder,
        DbDataSource dataSource,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
    {
        Check.NotNull(optionsBuilder, nameof(optionsBuilder));
        Check.NotNull(dataSource, nameof(dataSource));


        ConfigureWarnings(optionsBuilder);

        h2OptionsAction?.Invoke(new H2DbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    /// <summary>
    ///     <para>
    ///         Configures the context to connect to an H2 database, but without initially setting any
    ///         <see cref="DbConnection" /> or connection string.
    ///     </para>
    ///     <para>
    ///         The connection or connection string must be set explicitly or registered in the DI
    ///         before the <see cref="DbContext" /> is used to connect to a database.
    ///         Set a connection using <see cref="RelationalDatabaseFacadeExtensions.SetDbConnection" />, or a
    ///         connection string using <see cref="RelationalDatabaseFacadeExtensions.SetConnectionString" />.
    ///         There is no data source overload: an H2 database is embedded and is addressed by its URL.
    ///     </para>
    /// </summary>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseH2<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseH2(
            (DbContextOptionsBuilder)optionsBuilder, h2OptionsAction);

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">A builder for setting options on the context.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-configuration.</param>
    /// <returns>
    ///     The options builder so that further configuration can be chained.
    /// </returns>
    public static DbContextOptionsBuilder<TContext> UseH2<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string? connectionString,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseH2(
            (DbContextOptionsBuilder)optionsBuilder, connectionString, h2OptionsAction);

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">A builder for setting options on the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed. The caller owns the connection and is
    ///     responsible for its disposal.
    /// </param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>
    ///     The options builder so that further configuration can be chained.
    /// </returns>
    public static DbContextOptionsBuilder<TContext> UseH2<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseH2(
            (DbContextOptionsBuilder)optionsBuilder, connection, h2OptionsAction);

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <typeparam name="TContext">The type of context to be configured.</typeparam>
    /// <param name="optionsBuilder">The builder being used to configure the context.</param>
    /// <param name="connection">
    ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
    ///     in the open state then EF will not open or close the connection. If the connection is in the closed
    ///     state then EF will open and close the connection as needed.
    /// </param>
    /// <param name="contextOwnsConnection">
    ///     If <see langword="true" />, then EF will take ownership of the connection and will
    ///     dispose it in the same way it would dispose a connection created by EF. If <see langword="false" />, then the caller still
    ///     owns the connection and is responsible for its disposal.
    /// </param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>The options builder so that further configuration can be chained.</returns>
    public static DbContextOptionsBuilder<TContext> UseH2<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbConnection connection,
        bool contextOwnsConnection,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseH2(
            (DbContextOptionsBuilder)optionsBuilder, connection, contextOwnsConnection, h2OptionsAction);

    /// <summary>
    ///     Configures the context to connect to a PostgreSQL database with H2.
    /// </summary>
    /// <param name="optionsBuilder">A builder for setting options on the context.</param>
    /// <param name="dataSource">A <see cref="DbDataSource" /> which will be used to get database connections.</param>
    /// <param name="h2OptionsAction">An optional action to allow additional H2-specific configuration.</param>
    /// <returns>
    ///     The options builder so that further configuration can be chained.
    /// </returns>
    public static DbContextOptionsBuilder<TContext> UseH2<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        DbDataSource dataSource,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null)
        where TContext : DbContext
        => (DbContextOptionsBuilder<TContext>)UseH2(
            (DbContextOptionsBuilder)optionsBuilder, dataSource, h2OptionsAction);

    /// <summary>
    ///     Returns an existing instance of <see cref="H2OptionsExtension" />, or a new instance if one does not exist.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="DbContextOptionsBuilder" /> to search.</param>
    /// <returns>
    ///     An existing instance of <see cref="H2OptionsExtension" />, or a new instance if one does not exist.
    /// </returns>
    private static H2OptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.Options.FindExtension<H2OptionsExtension>() is { } existing
            ? new H2OptionsExtension(existing)
            : new H2OptionsExtension();

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        var coreOptionsExtension = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
            ?? new CoreOptionsExtension();

        coreOptionsExtension = RelationalOptionsExtension.WithDefaultWarningConfiguration(coreOptionsExtension);

        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
    }
}
