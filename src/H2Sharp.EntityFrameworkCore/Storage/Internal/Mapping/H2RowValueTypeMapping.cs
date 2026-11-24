using System;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     TODO: Update
///     Every node in the SQL tree must have a type mapping, but row values aren't actual values (in the sense that they can be sent as
///     parameters, or have a literal representation). So we have a dummy type mapping for that.
/// </summary>
public class H2RowValueTypeMapping : RelationalTypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2RowValueTypeMapping(Type clrType)
        : base(
            new RelationalTypeMappingParameters(
                new CoreTypeMappingParameters(clrType, converter: CreateConverter(clrType)),
                storeType: "record"))
    {
    }

    // The ADO reader hands a row value back as object[], while the shaper asks for the CLR tuple
    // the query projected. Rebuild the tuple from the array on the way from the provider.
    private static ValueConverter CreateConverter(Type clrType)
    {
        var providerType = typeof(object[]);
        var model = Expression.Parameter(clrType, "value");
        var provider = Expression.Parameter(providerType, "values");

        var toProvider = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(clrType, providerType),
            Expression.Block(
                Expression.Throw(
                    Expression.New(
                        typeof(InvalidOperationException).GetConstructor([typeof(string)])!,
                        Expression.Constant("Row values cannot be sent as parameters"))),
                Expression.Default(providerType)),
            model);

        var fields = clrType.GetGenericArguments();
        var constructor = clrType.GetConstructor(fields)
            ?? throw new InvalidOperationException($"No tuple constructor on {clrType}");

        var fromProvider = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(providerType, clrType),
            Expression.New(
                constructor,
                fields.Select(
                    (field, index) => Expression.Convert(
                        Expression.ArrayIndex(provider, Expression.Constant(index)),
                        field))),
            provider);

        return (ValueConverter)Activator.CreateInstance(
            typeof(ValueConverter<,>).MakeGenericType(clrType, providerType),
            toProvider,
            fromProvider,
            null)!;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2RowValueTypeMapping(RelationalTypeMappingParameters parameters)
        : base(parameters)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new H2RowValueTypeMapping(parameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    // SQL generation for row values is in H2QuerySqlGenerator
    protected override string GenerateNonNullSqlLiteral(object value)
        => throw new InvalidOperationException("GenerateNonNullSqlLiteral not supported on H2RowValueTypeMapping");

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override void ConfigureParameter(DbParameter parameter)
        => throw new InvalidOperationException("ConfigureParameter not supported on H2RowValueTypeMapping");
}
