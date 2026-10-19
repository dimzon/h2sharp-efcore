using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace H2Sharp.EntityFrameworkCore.Migrations.Internal;

// Migrator is EF-pubternal, but overriding it is the only way to force H2 to ReloadTypes() after executing a migration which adds
// types to the database
#pragma warning disable EF1001

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2Migrator : Migrator
{

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2Migrator(
        IMigrationsAssembly migrationsAssembly,
        IHistoryRepository historyRepository,
        IDatabaseCreator databaseCreator,
        IMigrationsSqlGenerator migrationsSqlGenerator,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IMigrationCommandExecutor migrationCommandExecutor,
        IRelationalConnection connection,
        ISqlGenerationHelper sqlGenerationHelper,
        ICurrentDbContext currentContext,
        IModelRuntimeInitializer modelRuntimeInitializer,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger,
        IRelationalCommandDiagnosticsLogger commandLogger,
        IDatabaseProvider databaseProvider,
        IMigrationsModelDiffer migrationsModelDiffer,
        IDesignTimeModel designTimeModel,
        IDbContextOptions contextOptions,
        IExecutionStrategy executionStrategy)
        : base(migrationsAssembly, historyRepository, databaseCreator, migrationsSqlGenerator, rawSqlCommandBuilder,
            migrationCommandExecutor, connection, sqlGenerationHelper, currentContext, modelRuntimeInitializer, logger,
            commandLogger, databaseProvider, migrationsModelDiffer, designTimeModel, contextOptions, executionStrategy)
    {
        // Upstream this constructor recorded the applied and reverted migrations and, when one of
        // them added an enum, a range or an extension, reconnected to reload the driver's type cache.
        // H2Sharp has no such cache: an H2 type is created by CREATE TYPE and read back from the
        // catalogue on demand, so there is nothing to invalidate and nothing to reload.
    }
}
