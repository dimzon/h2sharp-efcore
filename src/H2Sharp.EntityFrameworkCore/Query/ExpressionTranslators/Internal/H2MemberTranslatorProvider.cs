using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     A composite member translator that dispatches to multiple specialized member translators specific to H2.
/// </summary>
public class H2MemberTranslatorProvider : RelationalMemberTranslatorProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual H2JsonPocoTranslator JsonPocoTranslator { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2MemberTranslatorProvider(
        RelationalMemberTranslatorProviderDependencies dependencies,
        IModel model,
        IRelationalTypeMappingSource typeMappingSource,
        IDbContextOptions contextOptions)
        : base(dependencies)
    {
        var npgsqlOptions = contextOptions.FindExtension<H2OptionsExtension>() ?? new H2OptionsExtension();
        var supportsMultiranges = npgsqlOptions.PostgresVersion.AtLeast(14);

        var sqlExpressionFactory = (H2SqlExpressionFactory)dependencies.SqlExpressionFactory;
        JsonPocoTranslator = new H2JsonPocoTranslator(typeMappingSource, sqlExpressionFactory, model);

        AddTranslators(
        [
            new H2BigIntegerMemberTranslator(sqlExpressionFactory),
                new H2DateTimeMemberTranslator(typeMappingSource, sqlExpressionFactory),
                new H2JsonDomTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2LTreeTranslator(typeMappingSource, sqlExpressionFactory, model),
                JsonPocoTranslator,
                new H2RangeTranslator(typeMappingSource, sqlExpressionFactory, model, supportsMultiranges),
                new H2StringMemberTranslator(sqlExpressionFactory),
                new H2TimeSpanMemberTranslator(sqlExpressionFactory)
        ]);
    }
}
