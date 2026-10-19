using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using H2Sharp.Data;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Empties a test database between tests.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this dropped the database's extensions, enum types, functions and collations one
///         by one, reading the catalogue to find them, and then let EF Core's own cleaner drop the
///         tables it found in the database model.
///     </para>
///     <para>
///         None of that applies to H2. There are no extensions, no user-defined functions, and the
///         collations are the engine's own. And the catalogue EF Core's cleaner works from is
///         INFORMATION_SCHEMA, which the port does not have -- so a model built from the catalogue
///         would be empty and the cleaner would quietly drop nothing.
///     </para>
///     <para>
///         So the tables are dropped by name, from the model EF itself has: the tables the context
///         maps are exactly the tables that were created, and EF knows their names without asking
///         the database. H2's own "DROP ALL OBJECTS" would be the shorter way to say this, and the
///         cleaner will use it once the engine's parser reads it.
///     </para>
/// </remarks>
public class H2DatabaseCleaner : RelationalDatabaseCleaner
{
    /// <inheritdoc />
    protected override IDatabaseModelFactory CreateDatabaseModelFactory(ILoggerFactory loggerFactory)
        // The base cleaner builds a database model to find the tables to drop. H2's catalogue is
        // INFORMATION_SCHEMA, which the port does not have yet, so the model would come out empty.
        // The cleaner does not use it -- the tables come from EF's own model -- but the abstract
        // member still has to be answered.
        => throw new NotSupportedException(
            "The database model factory is not used: H2DatabaseCleaner drops the tables EF's model " +
            "knows, which does not need a catalogue.");

    /// <inheritdoc />
    public override void Clean(DatabaseFacade facade)
    {
        var connection = facade.GetService<IRelationalConnection>();

        if (connection.DbConnection is not H2Connection h2Connection)
        {
            base.Clean(facade);
            return;
        }

        var context = facade.GetService<ICurrentDbContext>().Context;
        var sqlGenerationHelper = facade.GetService<ISqlGenerationHelper>();

        var wasOpen = h2Connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            h2Connection.Open();
        }

        try
        {
            foreach (var table in context.Model.GetRelationalModel().Tables)
            {
                using var command = h2Connection.CreateCommand();
                command.CommandText = $"DROP TABLE IF EXISTS {sqlGenerationHelper.DelimitIdentifier(table.Name, table.Schema)}";
                command.ExecuteNonQuery();
            }
        }
        finally
        {
            if (!wasOpen)
            {
                h2Connection.Close();
            }
        }
    }
}
