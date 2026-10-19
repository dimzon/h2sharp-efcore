using System.Data.Common;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class TestRelationalTransactionFactory(RelationalTransactionFactoryDependencies dependencies) : IRelationalTransactionFactory
{
    protected virtual RelationalTransactionFactoryDependencies Dependencies { get; } = dependencies;

    public RelationalTransaction Create(
        IRelationalConnection connection,
        DbTransaction transaction,
        Guid transactionId,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger,
        bool transactionOwned)
        => new TestRelationalTransaction(connection, transaction, logger, transactionOwned, Dependencies.SqlGenerationHelper);
}

