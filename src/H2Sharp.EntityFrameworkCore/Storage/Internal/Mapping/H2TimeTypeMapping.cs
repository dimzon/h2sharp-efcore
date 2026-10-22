using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Storage.Json;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2TimeTypeMapping : H2TypeMapping
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static H2TimeTypeMapping Default { get; } = new(typeof(TimeOnly));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2TimeTypeMapping(Type clrType)
        : base(
            "time",
            clrType,
            clrType == typeof(TimeOnly)
                ? JsonTimeOnlyReaderWriter.Instance
                : clrType == typeof(TimeSpan)
                    ? JsonTimeSpanReaderWriter.Instance
                    : throw new ArgumentException("clrType must be TimeOnly or TimeSpan", nameof(clrType)))
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected H2TimeTypeMapping(RelationalTypeMappingParameters parameters)
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
        => new H2TimeTypeMapping(parameters);

    /// <inheritdoc />
    public override MethodInfo GetDataReaderMethod()
        => typeof(DbDataReader).GetMethod(nameof(DbDataReader.GetFieldValue))!.MakeGenericMethod(typeof(TimeSpan));

    /// <inheritdoc />
    public override Expression CustomizeDataReaderExpression(Expression expression)
        => (Converter?.ProviderClrType ?? ClrType) == typeof(TimeOnly)
            ? Expression.Call(typeof(TimeOnly).GetMethod(nameof(TimeOnly.FromTimeSpan), [typeof(TimeSpan)])!, expression)
            : expression;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string ProcessStoreType(RelationalTypeMappingParameters parameters, string storeType, string _)
        => parameters.Precision is null ? storeType : $"time({parameters.Precision}) without time zone";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
        => $"TIME '{GenerateEmbeddedNonNullSqlLiteral(value)}'";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateEmbeddedNonNullSqlLiteral(object value)
        => value switch
        {
            TimeSpan ts => ts.Ticks % 10000000 == 0
                ? ts.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture)
                : ts.ToString(@"hh\:mm\:ss\.FFFFFF", CultureInfo.InvariantCulture),
            TimeOnly t => t.Ticks % 10000000 == 0
                ? t.ToString(@"HH\:mm\:ss", CultureInfo.InvariantCulture)
                : t.ToString(@"HH\:mm\:ss\.FFFFFF", CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"Can't generate a time SQL literal for CLR type {value.GetType()}")
        };

    /// <summary>
    ///     The parameter that carries the time to the database.
    /// </summary>
    /// <remarks>
    ///     The ADO layer of the engine reads a time back as a <see cref="TimeSpan" />, but it takes
    ///     a time into the database as a <see cref="TimeOnly" />: a <see cref="TimeSpan" /> would
    ///     arrive as an interval, and the engine refuses to put an interval into a time. The value
    ///     that comes from the conversion of the model type is therefore turned back into the type
    ///     the engine takes.
    /// </remarks>
    /// <param name="command">The command the parameter belongs to.</param>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="nullable">Whether the parameter holds nothing.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <returns>The parameter.</returns>
    public override DbParameter CreateParameter(
        DbCommand command,
        string name,
        object? value,
        bool? nullable = null,
        ParameterDirection direction = ParameterDirection.Input)
    {
        var parameter = base.CreateParameter(command, name, value, nullable, direction);

        // The conversion of the model type has already turned the time into a TimeSpan by now,
        // and the ADO layer of the engine takes a time into the database as a TimeOnly: a
        // TimeSpan would arrive as an interval, and the engine refuses to put an interval into
        // a time. The value of the parameter is therefore turned back into the type the engine
        // takes.
        if (parameter.Value is TimeSpan span)
        {
            parameter.Value = TimeOnly.FromTimeSpan(span);
        }

        return parameter;
    }
}
