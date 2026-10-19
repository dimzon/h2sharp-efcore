namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public class TestH2RetryingExecutionStrategy : H2RetryingExecutionStrategy
{
    private const bool ErrorNumberDebugMode = false;

    private static readonly string[] AdditionalSqlStates = ["XX000"];

    public TestH2RetryingExecutionStrategy()
        : base(
            new DbContext(
                new DbContextOptionsBuilder()
                    .EnableServiceProviderCaching(false)
                    .UseH2(TestEnvironment.DefaultConnection).Options),
            DefaultMaxRetryCount, DefaultMaxDelay, AdditionalSqlStates)
    {
    }

    public TestH2RetryingExecutionStrategy(DbContext context)
        : base(context, DefaultMaxRetryCount, DefaultMaxDelay, AdditionalSqlStates)
    {
    }

    public TestH2RetryingExecutionStrategy(DbContext context, TimeSpan maxDelay)
        : base(context, DefaultMaxRetryCount, maxDelay, AdditionalSqlStates)
    {
    }

    // ReSharper disable once UnusedMember.Global
    public TestH2RetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
        : base(dependencies, DefaultMaxRetryCount, DefaultMaxDelay, AdditionalSqlStates)
    {
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        if (base.ShouldRetryOn(exception))
        {
            return true;
        }

#pragma warning disable 162
        if (ErrorNumberDebugMode && exception is PostgresException postgresException)
        {
            var message = $"Didn't retry on {postgresException.SqlState}";
            throw new InvalidOperationException(message, exception);
        }
#pragma warning restore 162

        return exception is InvalidOperationException { Message: "Internal .Net Framework Data Provider error 6." };
    }

    public new virtual TimeSpan? GetNextDelay(Exception lastException)
    {
        ExceptionsEncountered.Add(lastException);
        return base.GetNextDelay(lastException);
    }
}
