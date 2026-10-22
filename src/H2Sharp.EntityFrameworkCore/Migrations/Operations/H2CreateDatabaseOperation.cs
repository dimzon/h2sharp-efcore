namespace H2Sharp.EntityFrameworkCore.Migrations.Operations;

/// <summary>
///     A PostgreSQL-specific <see cref="MigrationOperation" /> to create a database.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-migrations">Database migrations</see>.
/// </remarks>
[DebuggerDisplay("CREATE DATABASE {Name}")]
public class H2CreateDatabaseOperation : DatabaseOperation
{
    /// <summary>
    ///     The name of the database.
    /// </summary>
    public virtual string Name { get; set; } = null!;

    /// <summary>
    ///     The database to use as a template for the new database to be created.
    /// </summary>
    public virtual string? Template { get; set; }
}
