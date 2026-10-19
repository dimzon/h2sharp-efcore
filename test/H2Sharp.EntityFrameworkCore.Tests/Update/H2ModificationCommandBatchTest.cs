using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Update.Internal;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using H2Sharp.EntityFrameworkCore.Update.Internal;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Tests.Update;

public class H2ModificationCommandBatchTest
{
    [Fact]
    public void AddCommand_returns_false_when_max_batch_size_is_reached()
    {
        var typeMapper = new H2TypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            new H2SqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()),
            new H2SingletonOptions());

        var batch = new H2ModificationCommandBatch(
            new ModificationCommandBatchFactoryDependencies(
                new RelationalCommandBuilderFactory(
                    new RelationalCommandBuilderDependencies(
                        typeMapper,
                        new ExceptionDetector())),
                new H2SqlGenerationHelper(
                    new RelationalSqlGenerationHelperDependencies()),
                new H2UpdateSqlGenerator(
                    new UpdateSqlGeneratorDependencies(
                        new H2SqlGenerationHelper(
                            new RelationalSqlGenerationHelperDependencies()),
                        typeMapper)),
                new CurrentDbContext(new FakeDbContext()),
                new FakeRelationalCommandDiagnosticsLogger(),
                new FakeDiagnosticsLogger<DbLoggerCategory.Update>()),
            maxBatchSize: 1);

        Assert.True(
            batch.TryAddCommand(
                CreateModificationCommand("T1", null, false)));
        Assert.False(
            batch.TryAddCommand(
                CreateModificationCommand("T1", null, false)));
    }

    private class FakeDbContext : DbContext;

    private static INonTrackedModificationCommand CreateModificationCommand(
        string name,
        string schema,
        bool sensitiveLoggingEnabled)
        => new ModificationCommandFactory().CreateNonTrackedModificationCommand(
            new NonTrackedModificationCommandParameters(name, schema, sensitiveLoggingEnabled));
}
