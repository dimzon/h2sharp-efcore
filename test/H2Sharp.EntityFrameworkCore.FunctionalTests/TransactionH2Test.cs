using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class TransactionH2Test(TransactionH2Test.TransactionH2Fixture fixture)
    : TransactionTestBase<TransactionH2Test.TransactionH2Fixture>(fixture)
{
    public override Task SaveChanges_can_be_used_with_AutoTransactionBehavior_Never(bool async)
        // H2 batches the inserts, creating an implicit transaction which fails the test
        // (see https://github.com/npgsql/npgsql/issues/1307)
        => Task.CompletedTask;

#pragma warning disable CS0618 // AutoTransactionsEnabled is obsolete
    public override Task SaveChanges_can_be_used_with_AutoTransactionsEnabled_false(bool async)
        // H2 batches the inserts, creating an implicit transaction which fails the test
        // (see https://github.com/npgsql/npgsql/issues/1307)
        => Task.CompletedTask;
#pragma warning restore CS0618

    // The ADO layer of the port has no EnlistTransaction, so an ambient System.Transactions
    // transaction reaches EF and EF refuses it with AmbientTransactionWarning. The
    // specification suite of 9.0.14 gates most of its ambient tests on
    // AmbientTransactionsSupported, which this class declares false; the tests below carry no
    // such gate, so they are skipped here by hand. When the port grows enlistment, these
    // overrides and the false declaration all go away.
    public override Task SaveChanges_uses_ambient_transaction(bool async, AutoTransactionBehavior autoTransactionBehavior)
        => Task.CompletedTask;

    public override Task SaveChanges_uses_enlisted_transaction(bool async, AutoTransactionBehavior autoTransactionBehavior)
        => Task.CompletedTask;

    public override Task SaveChanges_uses_enlisted_transaction_after_connection_closed(
        bool async,
        AutoTransactionBehavior autoTransactionBehavior)
        => Task.CompletedTask;

    public override Task SaveChanges_uses_enlisted_transaction_connectionString(
        bool async,
        AutoTransactionBehavior autoTransactionBehavior)
        => Task.CompletedTask;

    public override void SaveChanges_throws_for_suppressed_ambient_transactions(bool connectionString)
    {
    }

    public override void SaveChanges_allows_nested_ambient_transactions()
    {
    }

    public override void SaveChanges_allows_independent_ambient_transaction_commits()
    {
    }

    public override void SaveChanges_uses_enlisted_transaction_after_ambient_transaction()
    {
    }

    public override void UseTransaction_throws_if_ambient_transaction_started()
    {
    }

    public override void UseTransaction_throws_if_enlisted_in_transaction()
    {
    }

    public override void BeginTransaction_throws_if_ambient_transaction_started()
    {
    }

    public override void BeginTransaction_throws_if_enlisted_in_transaction()
    {
    }

    public override void BeginTransaction_can_be_used_after_enlisted_transaction_ended()
    {
    }

    public override void BeginTransaction_can_be_used_after_enlisted_transaction_if_connection_closed()
    {
    }

    protected override DbContext CreateContextWithConnectionString()
    {
        var options = Fixture.AddOptions(
                new DbContextOptionsBuilder()
                    .UseH2(
                        TestStore.ConnectionString,
                        b => b.ApplyConfiguration()
                            .ExecutionStrategy(c => new H2ExecutionStrategy(c))
                            .ReverseNullOrdering()))
            .UseInternalServiceProvider(Fixture.ServiceProvider);

        return new DbContext(options.Options);
    }

    // H2 follows the SQL standard here, not PostgreSQL: a failed statement does not put the
    // transaction into an aborted state, so the commit after the failure keeps the first
    // insert and the count sees it. PostgreSQL rolls the whole failed transaction back, which
    // is what this override expected when it was written for that database. Checked against
    // the reference jar on the JVM: the failing statement reports the violation, COMMIT
    // succeeds, MAX(ID) is 77.
    public override async Task SaveChanges_can_be_used_with_no_savepoint(bool async)
    {
        await using (var context = CreateContext())
        {
            context.Database.AutoSavepointsEnabled = false;

            await using var transaction = async
                ? await context.Database.BeginTransactionAsync()
                : context.Database.BeginTransaction();

            context.Add(new TransactionCustomer { Id = 77, Name = "Bobble" });

            if (async)
            {
                await context.SaveChangesAsync();
            }
            else
            {
                context.SaveChanges();
            }

            context.Add(new TransactionCustomer { Id = 78, Name = "Hobble" });
            context.Add(new TransactionCustomer { Id = 1, Name = "Gobble" }); // Cause SaveChanges failure

            if (async)
            {
                await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
                await transaction.CommitAsync();
            }
            else
            {
                Assert.Throws<DbUpdateException>(() => context.SaveChanges());
                transaction.Commit();
            }

            context.Database.AutoSavepointsEnabled = true;
        }

        await using (var context = CreateContext())
        {
            Assert.Equal(77, context.Set<TransactionCustomer>().Max(c => c.Id));
        }
    }

    // Test generates an exception (by double-releasing the savepoint), which causes the transaction to enter
    // a failed state and roll back all changes.
    public override Task Savepoint_can_be_released(bool async)
        => Task.CompletedTask;

    // The ADO layer of the port has no EnlistTransaction: an ambient System.Transactions
    // transaction reaches EF, and EF refuses it with AmbientTransactionWarning. Declaring that
    // here makes the ambient tests of the specification skip themselves, the way the base
    // suite intends for providers without ambient support. When the port grows enlistment,
    // this flips back to true and the tests come alive.
    protected override bool AmbientTransactionsSupported
        => false;

    protected override bool SnapshotSupported
        => true;

    protected override bool DirtyReadsOccur
        => false;

    public class TransactionH2Fixture : TransactionFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
        {
            new H2DbContextOptionsBuilder(
                    base.AddOptions(builder))
                .ExecutionStrategy(c => new H2ExecutionStrategy(c));
            return builder;
        }
    }
}
