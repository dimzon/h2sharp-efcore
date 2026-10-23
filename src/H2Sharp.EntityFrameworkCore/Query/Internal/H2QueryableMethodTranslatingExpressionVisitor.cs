using System.Diagnostics.CodeAnalysis;
using H2Sharp.EntityFrameworkCore.Extensions.Internal;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;
using H2Sharp.EntityFrameworkCore.Query.Expressions;
using H2Sharp.EntityFrameworkCore.Query.Expressions.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2QueryableMethodTranslatingExpressionVisitor : RelationalQueryableMethodTranslatingExpressionVisitor
{
    private readonly RelationalQueryCompilationContext _queryCompilationContext;
    private readonly H2TypeMappingSource _typeMappingSource;
    private readonly H2SqlExpressionFactory _sqlExpressionFactory;
    private readonly bool _isRedshift;
    private RelationalTypeMapping? _ordinalityTypeMapping;

    #region MethodInfos

    private static readonly MethodInfo Like2MethodInfo =
        typeof(DbFunctionsExtensions).GetRuntimeMethod(
            nameof(DbFunctionsExtensions.Like), [typeof(DbFunctions), typeof(string), typeof(string)])!;

    // ReSharper disable once InconsistentNaming
    private static readonly MethodInfo ILike2MethodInfo
        = typeof(H2DbFunctionsExtensions).GetRuntimeMethod(
            nameof(H2DbFunctionsExtensions.ILike), [typeof(DbFunctions), typeof(string), typeof(string)])!;

    #endregion

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2QueryableMethodTranslatingExpressionVisitor(
        QueryableMethodTranslatingExpressionVisitorDependencies dependencies,
        RelationalQueryableMethodTranslatingExpressionVisitorDependencies relationalDependencies,
        RelationalQueryCompilationContext queryCompilationContext,
        IH2SingletonOptions npgsqlSingletonOptions)
        : base(dependencies, relationalDependencies, queryCompilationContext)
    {
        _queryCompilationContext = queryCompilationContext;
        _typeMappingSource = (H2TypeMappingSource)relationalDependencies.TypeMappingSource;
        _sqlExpressionFactory = (H2SqlExpressionFactory)relationalDependencies.SqlExpressionFactory;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2QueryableMethodTranslatingExpressionVisitor(H2QueryableMethodTranslatingExpressionVisitor parentVisitor)
        : base(parentVisitor)
    {
        _queryCompilationContext = parentVisitor._queryCompilationContext;
        _typeMappingSource = parentVisitor._typeMappingSource;
        _sqlExpressionFactory = parentVisitor._sqlExpressionFactory;
        _isRedshift = parentVisitor._isRedshift;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override QueryableMethodTranslatingExpressionVisitor CreateSubqueryVisitor()
        => new H2QueryableMethodTranslatingExpressionVisitor(this);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslatePrimitiveCollection(
        SqlExpression sqlExpression,
        IProperty? property,
        string tableAlias)
    {
        if (_isRedshift)
        {
            AddTranslationErrorDetails("Redshift does not support unnest, which is required for most forms of querying of JSON arrays.");

            return null;
        }

        var elementClrType = sqlExpression.Type.GetSequenceType();
        var elementTypeMapping = (RelationalTypeMapping?)sqlExpression.TypeMapping?.ElementTypeMapping;

        // If this is a collection property, get the element's nullability out of metadata. Otherwise, this is a parameter property, in
        // which case we only have the CLR type (note that we cannot produce different SQLs based on the nullability of an *element* in
        // a parameter collection - our caching mechanism only supports varying by the nullability of the parameter itself (i.e. the
        // collection).
        // TODO: if property is non-null, GetElementType() should never be null, but we have #31469 for shadow properties
        var isElementNullable = property?.GetElementType() is null
            ? elementClrType.IsNullableType()
            : property.GetElementType()!.IsNullable;

        // Primitive collections are arrays, read with the unnest function. Geometry collections
        // have no table-valued decomposition in H2 2.5 and are declined below.
        SelectExpression selectExpression;

#pragma warning disable EF1001 // SelectExpression constructors are currently internal
        if (sqlExpression.TypeMapping is { StoreTypeNameBase: "geometry" })
        {
            AddTranslationErrorDetails("H2 2.5 has no table-valued function for decomposing geometry collections.");
            return null;
        }
            // Note that for unnest we have a special expression type extending TableValuedFunctionExpression, adding the ability to provide
            // an explicit column name for its output (SELECT * FROM unnest(array) AS f(foo)).
            // This is necessary since when the column name isn't explicitly specified, it is automatically identical to the table alias
            // (f above); since the table alias may get uniquified by EF, this would break queries.

            // TODO: When we have metadata to determine if the element is nullable, pass that here to SelectExpression

            // Note also that with the unnest of the fork, the output ordering is guaranteed to be the same as the input array. However, we still
            // need to add an explicit ordering on the ordinality column, since once the unnest is joined into a select, its "natural"
            // orderings is lost and an explicit ordering is needed again (see #3207).
            var (ordinalityColumn, ordinalityComparer) = GenerateOrdinalityIdentifier(tableAlias);
            selectExpression = new SelectExpression(
                [new H2UnnestExpression(tableAlias, sqlExpression, "value")],
                new ColumnExpression(
                    "value",
                    tableAlias,
                    elementClrType.UnwrapNullableType(),
                    elementTypeMapping,
                    isElementNullable),
                identifier: [(ordinalityColumn, ordinalityComparer)],
                _queryCompilationContext.SqlAliasManager);

            selectExpression.AppendOrdering(new OrderingExpression(ordinalityColumn, ascending: true));
#pragma warning restore EF1001

        Expression shaperExpression = new ProjectionBindingExpression(
            selectExpression, new ProjectionMember(), elementClrType.MakeNullable());

        if (elementClrType != shaperExpression.Type)
        {
            Check.DebugAssert(
                elementClrType.MakeNullable() == shaperExpression.Type,
                "expression.Type must be nullable of targetType");

            shaperExpression = Expression.Convert(shaperExpression, elementClrType);
        }

        return new ShapedQueryExpression(selectExpression, shaperExpression);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression TransformJsonQueryToTable(JsonQueryExpression jsonQueryExpression)
    {
        // H2 2.5 has no function that turns a JSON document into a table: jsonb_to_recordset and
        // json_to_recordset are PostgreSQL's.
        throw new NotSupportedException(
            "H2 2.5 cannot read a JSON document as a table: the functions jsonb_to_recordset and "
            + "json_to_recordset are PostgreSQL's.");
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateAll(ShapedQueryExpression source, LambdaExpression predicate)
    {
        if ((source.TryExtractArray(out var array, ignoreOrderings: true)
            || source.TryConvertValuesToArray(out array, ignoreOrderings: true))
            && source.QueryExpression is SelectExpression { Tables: [{ Alias: var tableAlias }] }
            && TranslateLambdaExpression(source, predicate) is { } translatedPredicate)
        {
            switch (translatedPredicate)
            {
                // Pattern match for: new[] { "a", "b", "c" }.All(p => EF.Functions.Like(e.SomeText, p)).
                // H2 has no quantified LIKE: a constant pattern list asks the same question with
                // AND, which H2 answers. Anything else would need UNNEST, so it stays refused below.
                case LikeExpression
                    {
                        Match: var match,
                        Pattern: ColumnExpression pattern,
                        EscapeChar: SqlConstantExpression { Value: "" }
                    }
                    when pattern.TableAlias == tableAlias && array is H2NewArrayExpression likeArray
                    && likeArray.Expressions.All(e => e is SqlConstantExpression):
                {
                    SqlExpression combined = _sqlExpressionFactory.Constant(true);

                    foreach (var element in likeArray.Expressions)
                    {
                        combined = _sqlExpressionFactory.AndAlso(combined, _sqlExpressionFactory.Like(match, element));
                    }

                    return BuildSimplifiedShapedQuery(source, combined);
                }

                // Pattern match for: new[] { "a", "b", "c" }.All(p => EF.Functions.ILike(e.SomeText, p)).
                // H2 has no quantified ILIKE either: a constant pattern list asks the same
                // question with AND, which H2 answers.
                case H2ILikeExpression
                    {
                        Match: var ilikeMatch,
                        Pattern: ColumnExpression ilikePattern,
                        EscapeChar: SqlConstantExpression { Value: "" }
                    }
                    when ilikePattern.TableAlias == tableAlias && array is H2NewArrayExpression ilikeArray
                    && ilikeArray.Expressions.All(e => e is SqlConstantExpression):
                {
                    SqlExpression ilikeCombined = _sqlExpressionFactory.Constant(true);

                    foreach (var element in ilikeArray.Expressions)
                    {
                        ilikeCombined = _sqlExpressionFactory.AndAlso(ilikeCombined, _sqlExpressionFactory.ILike(ilikeMatch, element));
                    }

                    return BuildSimplifiedShapedQuery(source, ilikeCombined);
                }

                // Pattern match for: e.SomeArray.All(p => ints.Contains(p)) over non-column.
                // Do not replace a quantified query with PostgreSQL's array-containment operator.
                // The relational translation preserves the predicate as a subquery instead.
                case H2AnyExpression
                    {
                        Item: ColumnExpression sourceColumn,
                        Array: var otherArray
                    }
                    when sourceColumn.TableAlias == tableAlias:
                {
                    return base.TranslateAll(source, predicate);
                }

                // Pattern match for: new[] { 4, 5 }.All(p => e.SomeArray.Contains(p)) over column.
                // Keep the quantified query rather than introducing a non-H2 operator.
                case H2BinaryExpression
                    {
                        OperatorType: H2ExpressionType.Contains,
                        Left: var otherArray,
                        Right: H2NewArrayExpression { Expressions: [ColumnExpression sourceColumn] }
                    }
                    when sourceColumn.TableAlias == tableAlias:
                {
                    return base.TranslateAll(source, predicate);
                }
            }
        }

        return base.TranslateAll(source, predicate);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateAny(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        if ((source.TryExtractArray(out var array, ignoreOrderings: true)
                || source.TryConvertValuesToArray(out array, ignoreOrderings: true))
            && source.QueryExpression is SelectExpression { Tables: [{ Alias: var tableAlias }] })
        {
            // Pattern match: x.Array.Any()
            // Translation: cardinality(x.array) > 0 instead of EXISTS (SELECT 1 FROM FROM unnest(x.Array))
            if (predicate is null)
            {
                return BuildSimplifiedShapedQuery(
                    source,
                    _sqlExpressionFactory.GreaterThan(
                        _sqlExpressionFactory.Function(
                            "cardinality",
                            [array],
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[1],
                            typeof(int)),
                        _sqlExpressionFactory.Constant(0)));
            }

            if (TranslateLambdaExpression(source, predicate) is not SqlExpression translatedPredicate)
            {
                return null;
            }

            switch (translatedPredicate)
            {
                // Pattern match: x.Array.Any(f => f == value).
                // H2 answers with ARRAY_CONTAINS, and no unnest is needed. The lambda is read
                // before translation here so that the element itself is recognized precisely:
                // anything else compared would change what the question asks. A "not equal"
                // check has no such form: it asks whether another element exists, not whether
                // the value is missing, so it stays refused below.
                case SqlBinaryExpression { OperatorType: ExpressionType.Equal } when predicate?.Body is BinaryExpression
                {
                    Left: var binaryLeft,
                    Right: var binaryRight
                } && predicate.Parameters is [var elementParameter]
                    && (ReferenceEquals(binaryLeft, elementParameter) ? binaryRight
                        : ReferenceEquals(binaryRight, elementParameter) ? binaryLeft : null) is { } otherExpression
                    && !RefersToParameter(elementParameter, otherExpression)
                    && TranslateExpression(otherExpression) is { } translatedOther:
                {
                    (translatedOther, array) = _sqlExpressionFactory.ApplyTypeMappingsOnItemAndArray(translatedOther, array);

                    return BuildSimplifiedShapedQuery(
                        source,
                        _sqlExpressionFactory.Function(
                            "array_contains",
                            [array, translatedOther],
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[2],
                            typeof(bool)));
                }

                // Pattern match: new[] { "a", "b", "c" }.Any(p => EF.Functions.Like(e.SomeText, p)).
                // H2 has no quantified LIKE: a constant pattern list asks the same question
                // with OR, which H2 answers. Anything else would need UNNEST, so it is refused
                // by falling through to the base translation below.
                case LikeExpression
                    {
                        Match: var match,
                        Pattern: ColumnExpression pattern,
                        EscapeChar: SqlConstantExpression { Value: "" }
                    }
                    when pattern.TableAlias == tableAlias && array is H2NewArrayExpression likeArray
                    && likeArray.Expressions.All(e => e is SqlConstantExpression):
                {
                    SqlExpression combined = _sqlExpressionFactory.Constant(false);

                    foreach (var element in likeArray.Expressions)
                    {
                        combined = _sqlExpressionFactory.OrElse(combined, _sqlExpressionFactory.Like(match, element));
                    }

                    return BuildSimplifiedShapedQuery(source, combined);
                }

                // Pattern match: new[] { "a", "b", "c" }.Any(p => EF.Functions.ILike(e.SomeText, p)).
                // H2 has no quantified ILIKE either: a constant pattern list asks the same
                // question with OR, which H2 answers.
                case H2ILikeExpression
                    {
                        Match: var ilikeMatch,
                        Pattern: ColumnExpression ilikePattern,
                        EscapeChar: SqlConstantExpression { Value: "" }
                    }
                    when ilikePattern.TableAlias == tableAlias && array is H2NewArrayExpression ilikeArray
                    && ilikeArray.Expressions.All(e => e is SqlConstantExpression):
                {
                    SqlExpression ilikeCombined = _sqlExpressionFactory.Constant(false);

                    foreach (var element in ilikeArray.Expressions)
                    {
                        ilikeCombined = _sqlExpressionFactory.OrElse(ilikeCombined, _sqlExpressionFactory.ILike(ilikeMatch, element));
                    }

                    return BuildSimplifiedShapedQuery(source, ilikeCombined);
                }

                // Array overlap over non-column
                // Pattern match: e.SomeArray.Any(p => ints.Contains(p)).
                // Keep the EXISTS query: H2's && operator compares geometry bounding boxes.
                case H2AnyExpression
                    {
                        Item: ColumnExpression sourceColumn,
                        Array: var otherArray
                    }
                    when sourceColumn.TableAlias == tableAlias:
                {
                    return base.TranslateAny(source, predicate);
                }

                // Array overlap over column
                // Pattern match: new[] { 4, 5 }.Any(p => e.SomeArray.Contains(p)).
                // Keep the EXISTS query rather than introducing PostgreSQL's array overlap.
                case H2BinaryExpression
                    {
                        OperatorType: H2ExpressionType.Contains,
                        Left: var otherArray,
                        Right: H2NewArrayExpression { Expressions: [ColumnExpression sourceColumn] }
                    }
                    when sourceColumn.TableAlias == tableAlias:
                {
                    return base.TranslateAny(source, predicate);
                }

            }
        }

        // Pattern match: x.Array1.Intersect(x.Array2).Any()
        // Translation: x.Array1 && x.Array2
        if (predicate is null
            && source.QueryExpression is SelectExpression
            {
                Tables:
                [
                    IntersectExpression
                    {
                        Source1:
                        {
                            Tables: [H2UnnestExpression { Array: var array1 }],
                            Predicate: null,
                            GroupBy: [],
                            Having: null,
                            IsDistinct: false,
                            Limit: null,
                            Offset: null
                        },
                        Source2:
                        {
                            Tables: [H2UnnestExpression { Array: var array2 }],
                            Predicate: null,
                            GroupBy: [],
                            Having: null,
                            IsDistinct: false,
                            Limit: null,
                            Offset: null
                        }
                    }
                ],
                GroupBy: [],
                Having: null,
                IsDistinct: false,
                Limit: null,
                Offset: null
            })
        {
            // Pattern match: x.Array1.Intersect(x.Array2).Any().
            // Retain the set operation and EXISTS instead of using a non-H2 array operator.
            return base.TranslateAny(source, predicate);
        }

        return base.TranslateAny(source, predicate);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateContains(ShapedQueryExpression source, Expression item)
    {
        // Note that most other simplifications convert ValuesExpression to unnest over array constructor, but we avoid doing that
        // here for Contains, since the relational translation for ValuesExpression is better.
        if (source.TryExtractArray(out var array, ignoreOrderings: true)
            && TranslateExpression(item, applyDefaultTypeMapping: false) is SqlExpression translatedItem)
        {
            (translatedItem, array) = _sqlExpressionFactory.ApplyTypeMappingsOnItemAndArray(translatedItem, array);

            // When the array is a column, we ask H2 whether the array holds the element.
            // H2 2.5 has no containment operator for arrays: "arr @> ARRAY[item]"
            // and the JSON "arr >> ARRAY[item]" are both unknown to it. What H2 has is
            // ARRAY_CONTAINS(arr, item), and it answers the nulls the way .NET's Contains
            // does: the reference answers TRUE for ARRAY_CONTAINS(ARRAY[1,NULL], NULL) and
            // FALSE for ARRAY_CONTAINS(ARRAY[1,2], NULL).
            switch (array)
            {
                case ColumnExpression:
                    return BuildSimplifiedShapedQuery(
                        source,
                        _sqlExpressionFactory.Function(
                            "array_contains",
                            [array, translatedItem],
                            nullable: true,
                            argumentsPropagateNullability: TrueArrays[2],
                            typeof(bool)));

                // For constant arrays (new[] { 1, 2, 3 }) or inline arrays (new[] { 1, param, 3 }), don't do anything PG-specific for since
                // the general EF Core mechanism is fine for that case: item IN (1, 2, 3).
                case SqlConstantExpression or H2NewArrayExpression:
                    break;

                // Similar to ParameterExpression below, but when a bare subquery is present inside ANY(), the comparison is
                // against each of its resulting rows (just like IN). To "extract" the array result of the scalar subquery, we need
                // to add an explicit cast (see #1803).
                case ScalarSubqueryExpression subqueryExpression:
                    return BuildSimplifiedShapedQuery(
                        source,
                        _sqlExpressionFactory.Any(
                            translatedItem,
                            _sqlExpressionFactory.Convert(
                                subqueryExpression, subqueryExpression.Type, subqueryExpression.TypeMapping),
                            H2AnyOperatorType.Equal));

                // For ParameterExpression, and for all other cases - e.g. array returned from some function -
                // translate to e.SomeText = ANY (@p). This is superior to the general solution which will expand
                // parameters to constants, since non-PG SQL does not support arrays.
                // Note that this will allow indexes on the item to be used.
                default:
                    return BuildSimplifiedShapedQuery(
                        source, _sqlExpressionFactory.Any(translatedItem, array, H2AnyOperatorType.Equal));
            }
        }

        return base.TranslateContains(source, item);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateCount(ShapedQueryExpression source, LambdaExpression? predicate)
    {
        // Simplify x.Array.Count() => cardinality(x.Array) instead of SELECT COUNT(*) FROM unnest(x.Array)
        if (predicate is null && source.TryExtractArray(out var array, ignoreOrderings: true))
        {
            var translation = _sqlExpressionFactory.Function(
                "cardinality",
                [array],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[1],
                typeof(int));

#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            return source.Update(
                new SelectExpression(translation, _queryCompilationContext.SqlAliasManager),
                Expression.Convert(
                    new ProjectionBindingExpression(source.QueryExpression, new ProjectionMember(), typeof(int?)),
                    typeof(int)));
#pragma warning restore EF1001
        }

        return base.TranslateCount(source, predicate);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression TranslateConcat(ShapedQueryExpression source1, ShapedQueryExpression source2)
    {
        // Simplify x.Array.Concat(y.Array) => x.Array || y.Array instead of:
        // SELECT u.value FROM unnest(x.Array) UNION ALL SELECT u.value FROM unnest(y.Array)
        if (source1.TryExtractArray(out var array1, out var projectedColumn1)
            && source2.TryExtractArray(out var array2, out var projectedColumn2))
        {
            Check.DebugAssert(projectedColumn1.Type == projectedColumn2.Type, "projectedColumn1.Type == projectedColumn2.Type");
            Check.DebugAssert(
                projectedColumn1.TypeMapping is not null || projectedColumn2.TypeMapping is not null,
                "Concat with no type mapping on either side (operation should be client-evaluated over parameters/constants");

            // TODO: Conflicting type mappings from both sides?
            var inferredTypeMapping = projectedColumn1.TypeMapping ?? projectedColumn2.TypeMapping;

#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            var tableAlias = ((SelectExpression)source1.QueryExpression).Tables.Single().Alias!;
            var selectExpression = new SelectExpression(
                [new H2UnnestExpression(tableAlias, _sqlExpressionFactory.Add(array1, array2), "value")],
                new ColumnExpression("value", tableAlias, projectedColumn1.Type, inferredTypeMapping, projectedColumn1.IsNullable || projectedColumn2.IsNullable),
                identifier: [GenerateOrdinalityIdentifier(tableAlias)],
                _queryCompilationContext.SqlAliasManager);
#pragma warning restore EF1001 // Internal EF Core API usage.

            // TODO: Simplify by using UpdateQueryExpression after https://github.com/dotnet/efcore/issues/31511
            Expression shaperExpression = new ProjectionBindingExpression(
                selectExpression, new ProjectionMember(), source1.ShaperExpression.Type.MakeNullable());

            if (source1.ShaperExpression.Type != shaperExpression.Type)
            {
                Check.DebugAssert(
                    source1.ShaperExpression.Type.MakeNullable() == shaperExpression.Type,
                    "expression.Type must be nullable of targetType");

                shaperExpression = Expression.Convert(shaperExpression, source1.ShaperExpression.Type);
            }

            return new ShapedQueryExpression(selectExpression, shaperExpression);
        }

        return base.TranslateConcat(source1, source2);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateElementAtOrDefault(
        ShapedQueryExpression source,
        Expression index,
        bool returnDefault)
    {
        // Simplify x.Array[1] => x.Array[1] (using the PG array subscript operator) instead of a subquery with LIMIT/OFFSET
        // Note that we have unnest over multiranges, not just arrays - but multiranges don't support subscripting/slicing.
        if (!returnDefault
            && source.TryExtractArray(out var array, out var projectedColumn)
            && TranslateExpression(index) is { } translatedIndex)
        {
            // The arrays of H2 2.5 are 1-based, so the index is adjusted.
#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            return source.UpdateQueryExpression(
                new SelectExpression(
                    _sqlExpressionFactory.ArrayIndex(
                        array,
                        GenerateOneBasedIndexExpression(translatedIndex), projectedColumn.IsNullable),
                _queryCompilationContext.SqlAliasManager));
#pragma warning restore EF1001
        }

        return base.TranslateElementAtOrDefault(source, index, returnDefault);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateFirstOrDefault(
        ShapedQueryExpression source,
        LambdaExpression? predicate,
        Type returnType,
        bool returnDefault)
    {
        // x.Array.First() is the first element: H2 arrays are 1-based, and reading past the
        // end fails in H2 the way First() fails on an empty input, so no unnest is needed.
        // FirstOrDefault stays refused: H2 answers an out-of-range read with an error, not NULL.
        if (!returnDefault
            && predicate is null
            && source.TryExtractArray(out var array, out var projectedColumn)
            && TranslateExpression(Expression.Constant(0)) is { } zero)
        {
#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            return source.UpdateQueryExpression(
                new SelectExpression(
                    _sqlExpressionFactory.ArrayIndex(
                        array,
                        GenerateOneBasedIndexExpression(zero), projectedColumn.IsNullable),
                    _queryCompilationContext.SqlAliasManager));
#pragma warning restore EF1001
        }

        return base.TranslateFirstOrDefault(source, predicate, returnType, returnDefault);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateSkip(ShapedQueryExpression source, Expression count)
    {
        // Translate Skip over array to the slice operator (array.Skip(2) -> array[3,])
        // Note that we have unnest over multiranges, not just arrays - but multiranges don't support subscripting/slicing.
        if (source.TryExtractArray(out var array, out var projectedColumn)
            && TranslateExpression(count) is { } translatedCount)
        {
#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            var tableAlias = ((SelectExpression)source.QueryExpression).Tables[0].Alias!;
            var selectExpression = new SelectExpression(
                [
                    new H2UnnestExpression(
                        tableAlias,
                        _sqlExpressionFactory.ArraySlice(
                            array,
                            lowerBound: GenerateOneBasedIndexExpression(translatedCount),
                            upperBound: null,
                            // isColumnNullable: /*projectedColumn.IsNullable*/ true, // TODO: This fails because of a shaper check
                            projectedColumn.IsNullable),
                        "value"),
                ],
                new ColumnExpression("value", tableAlias, projectedColumn.Type, projectedColumn.TypeMapping, projectedColumn.IsNullable),
                identifier: [GenerateOrdinalityIdentifier(tableAlias)],
                _queryCompilationContext.SqlAliasManager);
#pragma warning restore EF1001

            // TODO: Simplify by using UpdateQueryExpression after https://github.com/dotnet/efcore/issues/31511
            Expression shaperExpression = new ProjectionBindingExpression(
                selectExpression, new ProjectionMember(), source.ShaperExpression.Type.MakeNullable());

            if (source.ShaperExpression.Type != shaperExpression.Type)
            {
                Check.DebugAssert(
                    source.ShaperExpression.Type.MakeNullable() == shaperExpression.Type,
                    "expression.Type must be nullable of targetType");

                shaperExpression = Expression.Convert(shaperExpression, source.ShaperExpression.Type);
            }

            return new ShapedQueryExpression(selectExpression, shaperExpression);
        }

        return base.TranslateSkip(source, count);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateTake(ShapedQueryExpression source, Expression count)
    {
        // Translate Take over array to the slice operator (array.Take(2) -> array[,2])
        // Note that we have unnest over multiranges, not just arrays - but multiranges don't support subscripting/slicing.
        if (source.TryExtractArray(out var array, out var projectedColumn)
            && TranslateExpression(count) is { } translatedCount)
        {
            H2ArraySliceExpression sliceExpression;

            // If Skip has been called before, an array slice expression is already there; try to integrate this Take into it.
            // Note that we need to take the Skip (lower bound) into account for the Take (upper bound), since the slice upper bound
            // operates on the original array (Skip hasn't yet taken place).
            if (array is H2ArraySliceExpression existingSliceExpression)
            {
                if (existingSliceExpression is
                    {
                        LowerBound: SqlConstantExpression { Value: int lowerBoundValue } lowerBound,
                        UpperBound: null
                    })
                {
                    sliceExpression = existingSliceExpression.Update(
                        existingSliceExpression.Array,
                        existingSliceExpression.LowerBound,
                        translatedCount is SqlConstantExpression { Value: int takeCount }
                            ? _sqlExpressionFactory.Constant(lowerBoundValue + takeCount - 1, lowerBound.TypeMapping)
                            : _sqlExpressionFactory.Subtract(
                                _sqlExpressionFactory.Add(lowerBound, translatedCount),
                                _sqlExpressionFactory.Constant(1, lowerBound.TypeMapping)));
                }
                else
                {
                    // For any other case, we allow relational to translate with normal querying. For non-constant lower bounds, we could
                    // duplicate them into the upper bound, but that could cause expensive double evaluation.
                    return base.TranslateTake(source, count);
                }
            }
            else
            {
                sliceExpression = _sqlExpressionFactory.ArraySlice(
                    array,
                    lowerBound: null,
                    upperBound: translatedCount,
                    projectedColumn.IsNullable);
            }

#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            var tableAlias = ((SelectExpression)source.QueryExpression).Tables[0].Alias!;
            var selectExpression = new SelectExpression(
                [new H2UnnestExpression(tableAlias, sliceExpression, "value")],
                new ColumnExpression("value", tableAlias, projectedColumn.Type, projectedColumn.TypeMapping, projectedColumn.IsNullable),
                [GenerateOrdinalityIdentifier(tableAlias)],
                _queryCompilationContext.SqlAliasManager);
#pragma warning restore EF1001 // Internal EF Core API usage.

            // TODO: Simplify by using UpdateQueryExpression after https://github.com/dotnet/efcore/issues/31511
            Expression shaperExpression = new ProjectionBindingExpression(
                selectExpression, new ProjectionMember(), source.ShaperExpression.Type.MakeNullable());

            if (source.ShaperExpression.Type != shaperExpression.Type)
            {
                Check.DebugAssert(
                    source.ShaperExpression.Type.MakeNullable() == shaperExpression.Type,
                    "expression.Type must be nullable of targetType");

                shaperExpression = Expression.Convert(shaperExpression, source.ShaperExpression.Type);
            }

            return new ShapedQueryExpression(selectExpression, shaperExpression);
        }

        return base.TranslateTake(source, count);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override ShapedQueryExpression? TranslateWhere(ShapedQueryExpression source, LambdaExpression predicate)
    {
        // Simplify x.Array.Where(i => i != 3) => array_remove(x.Array, 3) instead of subquery
        if (predicate.Body is BinaryExpression
            {
                NodeType: ExpressionType.NotEqual,
                Left: var left,
                Right: var right
            }
            && (left == predicate.Parameters[0] ? right : right == predicate.Parameters[0] ? left : null) is Expression itemToFilterOut
            && source.TryExtractArray(out var array, out var projectedColumn)
            && TranslateExpression(itemToFilterOut) is SqlExpression translatedItemToFilterOut)
        {
            var simplifiedTranslation = _sqlExpressionFactory.Function(
                "array_remove",
                [array, translatedItemToFilterOut],
                nullable: true,
                argumentsPropagateNullability: TrueArrays[2],
                array.Type,
                array.TypeMapping);

#pragma warning disable EF1001 // SelectExpression constructors are currently internal
            var tableAlias = ((SelectExpression)source.QueryExpression).Tables[0].Alias!;
            var selectExpression = new SelectExpression(
                [new H2UnnestExpression(tableAlias, simplifiedTranslation, "value")],
                new ColumnExpression("value", tableAlias, projectedColumn.Type, projectedColumn.TypeMapping, projectedColumn.IsNullable),
                [GenerateOrdinalityIdentifier(tableAlias)],
                _queryCompilationContext.SqlAliasManager);
#pragma warning restore EF1001 // Internal EF Core API usage.

            // TODO: Simplify by using UpdateQueryExpression after https://github.com/dotnet/efcore/issues/31511
            Expression shaperExpression = new ProjectionBindingExpression(
                selectExpression, new ProjectionMember(), source.ShaperExpression.Type.MakeNullable());

            if (source.ShaperExpression.Type != shaperExpression.Type)
            {
                Check.DebugAssert(
                    source.ShaperExpression.Type.MakeNullable() == shaperExpression.Type,
                    "expression.Type must be nullable of targetType");

                shaperExpression = Expression.Convert(shaperExpression, source.ShaperExpression.Type);
            }

            return new ShapedQueryExpression(selectExpression, shaperExpression);
        }

        return base.TranslateWhere(source, predicate);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsNaturallyOrdered(SelectExpression selectExpression)
        => selectExpression is { Tables: [H2UnnestExpression unnest, ..] }
            && (selectExpression.Orderings is []
                || selectExpression.Orderings is
                    [{ Expression: ColumnExpression { Name: "ordinality", TableAlias: var orderingTableAlias } }]
                && orderingTableAlias == unnest.Alias);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsValidSelectExpressionForExecuteUpdate(
        SelectExpression selectExpression,
        TableExpressionBase targetTable,
        [NotNullWhen(true)] out TableExpression? tableExpression)
    {
        if (!base.IsValidSelectExpressionForExecuteUpdate(selectExpression, targetTable, out tableExpression))
        {
            return false;
        }

        // The main update table cannot be referenced from anywhere except for the UPDATE WHERE clause.
        // This specifically makes it impossible to have joins which reference the main table in their predicate (ON ...).
        // Because of this, we detect all such inner joins and lift their predicates to the main WHERE clause (where a reference to the
        // main table is allowed) - see H2QuerySqlGenerator.VisitUpdate.
        // For any other type of join which contains a reference to the main table, we return false to trigger a subquery pushdown instead.
        OuterReferenceFindingExpressionVisitor? visitor = null;

        for (var i = 0; i < selectExpression.Tables.Count; i++)
        {
            var table = selectExpression.Tables[i];

            if (ReferenceEquals(table, tableExpression))
            {
                continue;
            }

            visitor ??= new OuterReferenceFindingExpressionVisitor(tableExpression);

            // For inner joins, if the predicate contains a reference to the main table, H2QuerySqlGenerator will lift the predicate
            // to the WHERE clause; so we only need to check the inner join's table (i.e. subquery) for such a reference.
            // Cross join and cross/outer apply (lateral joins) don't have predicates, so just check the entire join for a reference to
            // the main table, and switch to subquery syntax if one is found.
            // Left join does have a predicate, but it isn't possible to lift it to the main WHERE clause; so also check the entire
            // join.
            if (table is InnerJoinExpression innerJoin)
            {
                table = innerJoin.Table;
            }

            if (visitor.ContainsReferenceToMainTable(table))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override bool IsValidSelectExpressionForExecuteDelete(
        SelectExpression selectExpression,
        StructuralTypeShaperExpression shaper,
        [NotNullWhen(true)] out TableExpression? tableExpression)
    {
        // A joined source must use EF's key-subquery fallback rather than PostgreSQL's DELETE USING.
        if (selectExpression.Tables.Count != 1)
        {
            tableExpression = null;
            return false;
        }

        // A direct DELETE requires a single table and a predicate.
        if (selectExpression is
            {
                Orderings: [],
                Offset: null,
                Limit: null,
                GroupBy: [],
                Having: null
            })
        {
            if (selectExpression.Tables[0] is TableExpression te)
            {
                tableExpression = te;
                return true;
            }
        }

        tableExpression = null;
        return false;
    }

    // The unnest of the fork is guaranteed to return output rows in the same order as its input array.
    /// <inheritdoc />
    protected override bool IsOrdered(SelectExpression selectExpression)
        => base.IsOrdered(selectExpression)
            || selectExpression.Tables is
                [H2TableValuedFunctionExpression { Name: "unnest" or "jsonb_to_recordset" or "json_to_recordset" }];

    private (ColumnExpression, ValueComparer) GenerateOrdinalityIdentifier(string tableAlias)
    {
        _ordinalityTypeMapping ??= _typeMappingSource.FindMapping("int")!;
        return (new ColumnExpression("ordinality", tableAlias, typeof(int), _ordinalityTypeMapping, nullable: false),
            _ordinalityTypeMapping.Comparer);
    }

    /// <summary>
    ///     The array indexing of H2 2.5 is 1-based. If the index happens to be a constant, just increment it. Otherwise, append a +1 in the
    ///     SQL.
    /// </summary>
    private SqlExpression GenerateOneBasedIndexExpression(SqlExpression expression)
        => expression is SqlConstantExpression constant
            ? _sqlExpressionFactory.Constant(Convert.ToInt32(constant.Value) + 1, constant.TypeMapping)
            : _sqlExpressionFactory.Add(expression, _sqlExpressionFactory.Constant(1));

#pragma warning disable EF1001 // SelectExpression constructors are currently internal
    private ShapedQueryExpression BuildSimplifiedShapedQuery(ShapedQueryExpression source, SqlExpression translation)
        => source.Update(
            new SelectExpression(translation, _queryCompilationContext.SqlAliasManager),
            Expression.Convert(
                new ProjectionBindingExpression(translation, new ProjectionMember(), typeof(bool?)), typeof(bool)));
#pragma warning restore EF1001

    private static bool RefersToParameter(ParameterExpression parameter, Expression expression)
    {
        var finder = new ParameterFindingVisitor(parameter);
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class ParameterFindingVisitor(ParameterExpression parameter) : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (ReferenceEquals(node, parameter))
            {
                Found = true;
            }

            return base.VisitParameter(node);
        }
    }

    private sealed class OuterReferenceFindingExpressionVisitor(TableExpression mainTable) : ExpressionVisitor
    {
        private bool _containsReference;

        public bool ContainsReferenceToMainTable(TableExpressionBase tableExpression)
        {
            _containsReference = false;

            Visit(tableExpression);

            return _containsReference;
        }

        [return: NotNullIfNotNull("expression")]
        public override Expression? Visit(Expression? expression)
        {
            if (_containsReference)
            {
                return expression;
            }

            if (expression is ColumnExpression { TableAlias: var tableAlias} && tableAlias == mainTable.Alias)
            {
                _containsReference = true;

                return expression;
            }

            return base.Visit(expression);
        }
    }
}
