using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Update.Internal;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using H2Sharp.EntityFrameworkCore.Update.Internal;

namespace H2Sharp.EntityFrameworkCore.Update;

public class H2ModificationCommandBatchFactoryTest
{
    [Fact]
    public void Uses_MaxBatchSize_specified_in_H2OptionsExtension()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseH2("Database=Crunchie", b => b.MaxBatchSize(1));

        var typeMapper = new H2TypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            new H2SqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()),
            new H2SingletonOptions());

        var factory = new H2ModificationCommandBatchFactory(
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
            optionsBuilder.Options);

        var batch = factory.Create();

        Assert.True(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
        Assert.False(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
    }

    [Fact]
    public void MaxBatchSize_is_optional()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        optionsBuilder.UseH2("Database=Crunchie");

        var typeMapper = new H2TypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            new H2SqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()),
            new H2SingletonOptions());

        var factory = new H2ModificationCommandBatchFactory(
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
            optionsBuilder.Options);

        var batch = factory.Create();

        Assert.True(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
        Assert.True(batch.TryAddCommand(CreateModificationCommand("T1", null, false)));
    }

    private class FakeDbContext : DbContext;

    private static INonTrackedModificationCommand CreateModificationCommand(
        string name,
        string schema,
        bool sensitiveLoggingEnabled)
        => new ModificationCommandFactory().CreateNonTrackedModificationCommand(
            new NonTrackedModificationCommandParameters(name, schema, sensitiveLoggingEnabled));
}
