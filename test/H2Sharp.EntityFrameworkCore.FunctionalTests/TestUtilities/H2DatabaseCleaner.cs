using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
///         INFORMATION_SCHEMA, which the port does not have yet -- so the model it would build is
///         empty and the cleaner would quietly drop nothing.
///     </para>
///     <para>
///         H2 has one statement for this: DROP ALL OBJECTS removes everything in the schema. That is
///         what this does, and it is why the cleaner does not go through a database model at all.
///     </para>
/// </remarks>
public class H2DatabaseCleaner : RelationalDatabaseCleaner
{
    /// <inheritdoc />
    protected override IDatabaseModelFactory CreateDatabaseModelFactory(ILoggerFactory loggerFactory)
        // The base cleaner builds a database model to find the tables to drop. H2's catalogue is
        // INFORMATION_SCHEMA, which the port does not have yet, so the model would come out empty.
        // The cleaner does not use it -- DROP ALL OBJECTS does the whole job -- but the abstract member
        // still has to be answered.
        => throw new NotSupportedException(
            "The database model factory is not used: H2DatabaseCleaner cleans with DROP ALL OBJECTS, " +
            "which does not need a catalogue.");

    /// <inheritdoc />
    public override void Clean(DatabaseFacade facade)
    {
        var connection = facade.GetService<IRelationalConnection>();

        if (connection.DbConnection is not H2Connection h2Connection)
        {
            base.Clean(facade);
            return;
        }

        var wasOpen = h2Connection.State == ConnectionState.Open;
        if (!wasOpen)
        {
            h2Connection.Open();
        }

        try
        {
            using var command = h2Connection.CreateCommand();
            command.CommandText = "DROP ALL OBJECTS";
            command.ExecuteNonQuery();
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
