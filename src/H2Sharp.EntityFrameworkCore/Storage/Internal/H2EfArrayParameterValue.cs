using System.Collections;
using System.Globalization;
using System.Linq;
using H2Sharp.Data;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     EF-facing wrapper for an SQL ARRAY parameter. The engine contract is the public base;
///     this subclass is internal so no provider-specific rendering leaks into the engine.
/// </summary>
internal sealed class H2EfArrayParameterValue : H2ArrayParameterValue
{
    private readonly object?[] values;

    internal H2EfArrayParameterValue(Type elementType, IEnumerable values)
    {
        this.values = [.. values.Cast<object?>()];
        ElementClrType = elementType;
    }

    public override Type ElementClrType { get; }

    public override IEnumerable<object?> Values
        => values;

    public override string ToString()
        => "{ " + string.Join(", ", values.Select(Format)) + " }";

    private static string Format(object? value)
        => "'" + (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Replace("'", "''") + "'";
}
