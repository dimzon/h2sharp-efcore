using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.Json;
using NodaTime.Text;
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using static H2Sharp.EntityFrameworkCore.NodaTime.Utilties.Util;

// ReSharper disable once CheckNamespace
namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class DateMapping : H2TypeMapping
{
    private static readonly ConstructorInfo Constructor =
        typeof(LocalDate).GetConstructor([typeof(int), typeof(int), typeof(int)])!;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public static DateMapping Default { get; } = new();

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public DateMapping()
        : base("date", typeof(LocalDate), H2DbType.Date, JsonLocalDateReaderWriter.Instance)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected DateMapping(RelationalTypeMappingParameters parameters)
        : base(parameters, H2DbType.Date)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new DateMapping(parameters);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override RelationalTypeMapping WithStoreTypeAndSize(string storeType, int? size)
        => new DateMapping(Parameters.WithStoreTypeAndSize(storeType, size));

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateNonNullSqlLiteral(object value)
        => $"DATE '{GenerateEmbeddedNonNullSqlLiteral(value)}'";

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected override string GenerateEmbeddedNonNullSqlLiteral(object value)
        => FormatLocalDate((LocalDate)value);

    private static string FormatLocalDate(LocalDate date)
    {
        if (!H2NodaTimeTypeMappingSourcePlugin.DisableDateTimeInfinityConversions)
        {
            if (date == LocalDate.MinIsoValue)
            {
                return "-infinity";
            }

            if (date == LocalDate.MaxIsoValue)
            {
                return "infinity";
            }
        }

        return LocalDatePattern.Iso.Format(date);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public override Expression GenerateCodeLiteral(object value)
    {
        var date = (LocalDate)value;
        return ConstantNew(Constructor, date.Year, date.Month, date.Day);
    }

    private sealed class JsonLocalDateReaderWriter : JsonValueReaderWriter<LocalDate>
    {
        private static readonly PropertyInfo InstanceProperty = typeof(JsonLocalDateReaderWriter).GetProperty(nameof(Instance))!;

        public static JsonLocalDateReaderWriter Instance { get; } = new();

        public override LocalDate FromJsonTyped(ref Utf8JsonReaderManager manager, object? existingObject = null)
        {
            var s = manager.CurrentReader.GetString()!;

            if (!H2NodaTimeTypeMappingSourcePlugin.DisableDateTimeInfinityConversions)
            {
                switch (s)
                {
                    case "-infinity":
                        return LocalDate.MinIsoValue;
                    case "infinity":
                        return LocalDate.MaxIsoValue;
                }
            }

            return LocalDatePattern.Iso.Parse(s).GetValueOrThrow();
        }

        public override void ToJsonTyped(Utf8JsonWriter writer, LocalDate value)
            => writer.WriteStringValue(FormatLocalDate(value));

        /// <inheritdoc />
        public override Expression ConstructorExpression => Expression.Property(null, InstanceProperty);
    }
}
