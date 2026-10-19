using H2Sharp.EntityFrameworkCore.Diagnostics.Internal;
using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Metadata.Conventions;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Migrations;
using H2Sharp.EntityFrameworkCore.Migrations.Internal;
using H2Sharp.EntityFrameworkCore.Query;
using H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using H2Sharp.EntityFrameworkCore.Query.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.Update.Internal;
using H2Sharp.EntityFrameworkCore.ValueGeneration.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     Provides extension methods to configure Entity Framework Core for H2.
/// </summary>
// ReSharper disable once UnusedMember.Global
public static class H2ServiceCollectionExtensions
{
    /// <summary>
    ///     <para>
    ///         Registers the given Entity Framework context as a service in the <see cref="IServiceCollection" />
    ///         and configures it to connect to a PostgreSQL database.
    ///     </para>
    ///     <para>
    ///         Use this method when using dependency injection in your application, such as with ASP.NET Core.
    ///         For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///         instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///         overridden to configure the SQL Server provider and connection string.
    ///     </para>
    ///     <para>
    ///         To configure the <see cref="DbContextOptions{TContext}" /> for the context, either override the
    ///         <see cref="DbContext.OnConfiguring" /> method in your derived context, or supply
    ///         an optional action to configure the <see cref="DbContextOptions" /> for the context.
    ///     </para>
    ///     <para>
    ///         For more information on how to use this method, see the Entity Framework Core documentation at https://aka.ms/efdocs.
    ///         For more information on using dependency injection, see https://go.microsoft.com/fwlink/?LinkId=526890.
    ///     </para>
    /// </summary>
    /// <typeparam name="TContext"> The type of context to be registered. </typeparam>
    /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
    /// <param name="connectionString"> The connection string of the database to connect to. </param>
    /// <param name="h2OptionsAction"> An optional action to allow additional SQL Server specific configuration. </param>
    /// <param name="optionsAction"> An optional action to configure the <see cref="DbContextOptions" /> for the context. </param>
    /// <returns> The same service collection so that multiple calls can be chained. </returns>
    public static IServiceCollection AddH2<TContext>(
        this IServiceCollection serviceCollection,
        string? connectionString,
        Action<H2DbContextOptionsBuilder>? h2OptionsAction = null,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
    {
        Check.NotNull(serviceCollection, nameof(serviceCollection));

        return serviceCollection.AddDbContext<TContext>(
            (_, options) =>
            {
                optionsAction?.Invoke(options);
                options.UseH2(connectionString, h2OptionsAction);
            });
    }

    /// <summary>
    ///     <para>
    ///         Adds the services required by the H2 database provider for Entity Framework
    ///         to an <see cref="IServiceCollection" />.
    ///     </para>
    ///     <para>
    ///         Calling this method is no longer necessary when building most applications, including those that
    ///         use dependency injection in ASP.NET or elsewhere.
    ///         It is only needed when building the internal service provider for use with
    ///         the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
    ///         This is not recommend other than for some advanced scenarios.
    ///     </para>
    /// </summary>
    /// <param name="serviceCollection"> The <see cref="IServiceCollection" /> to add services to. </param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    public static IServiceCollection AddEntityFrameworkH2(this IServiceCollection serviceCollection)
    {
        Check.NotNull(serviceCollection, nameof(serviceCollection));

        new EntityFrameworkH2ServicesBuilder(serviceCollection)
            .TryAdd<LoggingDefinitions, H2LoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<H2OptionsExtension>>()
            .TryAdd<IValueGeneratorCache>(p => p.GetRequiredService<IH2ValueGeneratorCache>())
            .TryAdd<IRelationalTypeMappingSource, H2TypeMappingSource>()
            .TryAdd<ISqlGenerationHelper, H2SqlGenerationHelper>()
            .TryAdd<IRelationalAnnotationProvider, H2AnnotationProvider>()
            .TryAdd<IModelValidator, H2ModelValidator>()
            .TryAdd<IMigrator, H2Migrator>()
            .TryAdd<IProviderConventionSetBuilder, H2ConventionSetBuilder>()
            .TryAdd<IUpdateSqlGenerator, H2UpdateSqlGenerator>()
            .TryAdd<IModificationCommandFactory, H2ModificationCommandFactory>()
            .TryAdd<IModificationCommandBatchFactory, H2ModificationCommandBatchFactory>()
            .TryAdd<IValueGeneratorSelector, H2ValueGeneratorSelector>()
            .TryAdd<IRelationalConnection>(p => p.GetRequiredService<IH2RelationalConnection>())
            .TryAdd<IMigrationsSqlGenerator, H2MigrationsSqlGenerator>()
            .TryAdd<IRelationalDatabaseCreator, H2DatabaseCreator>()
            .TryAdd<IHistoryRepository, H2HistoryRepository>()
            .TryAdd<ICompiledQueryCacheKeyGenerator, H2CompiledQueryCacheKeyGenerator>()
            .TryAdd<IExecutionStrategyFactory, H2ExecutionStrategyFactory>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, H2QueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IMethodCallTranslatorProvider, H2MethodCallTranslatorProvider>()
            .TryAdd<IAggregateMethodCallTranslatorProvider, H2AggregateMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, H2MemberTranslatorProvider>()
            .TryAdd<IEvaluatableExpressionFilter, H2EvaluatableExpressionFilter>()
            .TryAdd<IQuerySqlGeneratorFactory, H2QuerySqlGeneratorFactory>()
            .TryAdd<IRelationalSqlTranslatingExpressionVisitorFactory, H2SqlTranslatingExpressionVisitorFactory>()
            .TryAdd<IQueryTranslationPostprocessorFactory, H2QueryTranslationPostprocessorFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, H2ParameterBasedSqlProcessorFactory>()
            .TryAdd<ISqlExpressionFactory, H2SqlExpressionFactory>()
            .TryAdd<ISingletonOptions, IH2SingletonOptions>(p => p.GetRequiredService<IH2SingletonOptions>())
            .TryAdd<IQueryCompilationContextFactory, H2QueryCompilationContextFactory>()
            .TryAddProviderSpecificServices(
                b => b
                    .TryAddSingleton<IH2ValueGeneratorCache, H2ValueGeneratorCache>()
                    .TryAddSingleton<IH2SingletonOptions, H2SingletonOptions>()
                    .TryAddSingleton<IH2SequenceValueGeneratorFactory, H2SequenceValueGeneratorFactory>()
                    .TryAddScoped<IH2RelationalConnection, H2RelationalConnection>())
            .TryAddCoreServices();

        return serviceCollection;
    }
}
