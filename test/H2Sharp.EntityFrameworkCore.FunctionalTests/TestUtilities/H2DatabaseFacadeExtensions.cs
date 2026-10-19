namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public static class H2DatabaseFacadeExtensions
{
    public static void EnsureClean(this DatabaseFacade databaseFacade)
        => databaseFacade.CreateExecutionStrategy()
            .Execute(databaseFacade, database => new H2DatabaseCleaner().Clean(database));
}
