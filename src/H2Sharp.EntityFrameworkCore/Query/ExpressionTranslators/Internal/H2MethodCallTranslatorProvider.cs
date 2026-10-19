using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2MethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual H2LTreeTranslator LTreeTranslator { get; }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2MethodCallTranslatorProvider(
        RelationalMethodCallTranslatorProviderDependencies dependencies,
        IModel model,
        IDbContextOptions contextOptions)
        : base(dependencies)
    {
        var npgsqlOptions = contextOptions.FindExtension<H2OptionsExtension>() ?? new H2OptionsExtension();
        var supportsMultiranges = npgsqlOptions.PostgresVersion.AtLeast(14);

        var sqlExpressionFactory = (H2SqlExpressionFactory)dependencies.SqlExpressionFactory;
        var typeMappingSource = (H2TypeMappingSource)dependencies.RelationalTypeMappingSource;
        var jsonTranslator = new H2JsonPocoTranslator(typeMappingSource, sqlExpressionFactory, model);
        LTreeTranslator = new H2LTreeTranslator(typeMappingSource, sqlExpressionFactory, model);

        AddTranslators(
        [
            new H2ArrayMethodTranslator(sqlExpressionFactory, jsonTranslator),
                new H2ByteArrayMethodTranslator(sqlExpressionFactory),
                new H2ConvertTranslator(sqlExpressionFactory),
                new H2DateTimeMethodTranslator(typeMappingSource, sqlExpressionFactory),
                new H2FullTextSearchMethodTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2FuzzyStringMatchMethodTranslator(sqlExpressionFactory),
                new H2JsonDomTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2JsonDbFunctionsTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2JsonPocoTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2LikeTranslator(sqlExpressionFactory),
                LTreeTranslator,
                new H2MathTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2NetworkTranslator(typeMappingSource, sqlExpressionFactory, model),
                new H2NewGuidTranslator(sqlExpressionFactory, npgsqlOptions.PostgresVersion),
                new H2ObjectToStringTranslator(typeMappingSource, sqlExpressionFactory),
                new H2RandomTranslator(sqlExpressionFactory),
                new H2RangeTranslator(typeMappingSource, sqlExpressionFactory, model, supportsMultiranges),
                new H2RegexIsMatchTranslator(sqlExpressionFactory),
                new H2RowValueTranslator(sqlExpressionFactory),
                new H2StringMethodTranslator(typeMappingSource, sqlExpressionFactory),
                new H2TrigramsMethodTranslator(typeMappingSource, sqlExpressionFactory, model)
        ]);
    }
}
