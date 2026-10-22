using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
/// <remarks>
///     <para>
///         The translators upstream registered here that are gone: fuzzy string matching, trigram
///         similarity and regex are PostgreSQL built-ins with no H2 equivalent; the range and network
///         translators went with their types; ltree is an extension type H2 does not have. Full-text
///         search went with tsvector and tsquery.
///     </para>
/// </remarks>
public class H2MethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    public H2MethodCallTranslatorProvider(
        RelationalMethodCallTranslatorProviderDependencies dependencies,
        IModel model,
        IDbContextOptions contextOptions)
        : base(dependencies)
    {
        var h2Options = contextOptions.FindExtension<H2OptionsExtension>() ?? new H2OptionsExtension();

        var sqlExpressionFactory = (H2SqlExpressionFactory)dependencies.SqlExpressionFactory;
        var typeMappingSource = (H2TypeMappingSource)dependencies.RelationalTypeMappingSource;
        var jsonTranslator = new H2JsonPocoTranslator(typeMappingSource, sqlExpressionFactory, model);

        AddTranslators(
        [
            new H2ArrayMethodTranslator(sqlExpressionFactory, jsonTranslator),
            new H2BuiltinFunctionsTranslator(typeMappingSource, sqlExpressionFactory),
            new H2ByteArrayMethodTranslator(sqlExpressionFactory),
            new H2ConvertTranslator(sqlExpressionFactory),
            new H2DateTimeMethodTranslator(typeMappingSource, sqlExpressionFactory),
            new H2JsonDomTranslator(typeMappingSource, sqlExpressionFactory, model),
            new H2JsonPocoTranslator(typeMappingSource, sqlExpressionFactory, model),
            new H2LikeTranslator(sqlExpressionFactory),
            new H2MathTranslator(typeMappingSource, sqlExpressionFactory, model),
            new H2NewGuidTranslator(sqlExpressionFactory),
            new H2ObjectToStringTranslator(typeMappingSource, sqlExpressionFactory),
            new H2RandomTranslator(sqlExpressionFactory),
            new H2RegexIsMatchTranslator(sqlExpressionFactory),
            new H2RowValueTranslator(sqlExpressionFactory),
            new H2StringMethodTranslator(typeMappingSource, sqlExpressionFactory)
        ]);
    }
}
