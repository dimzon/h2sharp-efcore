using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace H2Sharp.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     The convention that gives the model the date and the time it asked for.
/// </summary>
/// <remarks>
///     <para>
///         The ADO layer of the engine hands a date back as a <see cref="DateTime" /> and a time
///         back as a <see cref="TimeSpan" />, the way the other ADO providers do (the reference
///         gives a <c>java.sql.Date</c> and a <c>java.sql.Time</c>). The model of the application
///         holds a <see cref="DateOnly" /> and a <see cref="TimeOnly" />, and the conversion
///         between the two is what stands between the column and the property.
///     </para>
///     <para>
///         The convention sets that conversion on the property, because that is where EF Core
///         applies it: on the way in, on the way out, and in the literal the migration writes.
///         The type of the column stays the type of H2 2.5: <c>date</c> and <c>time</c>.
///     </para>
/// </remarks>
public class H2DateAndTimeConvention : IPropertyAddedConvention
{
    /// <inheritdoc />
    public void ProcessPropertyAdded(IConventionPropertyBuilder propertyBuilder, IConventionContext<IConventionPropertyBuilder> context)
    {
        var property = propertyBuilder.Metadata;

        if (property.GetValueConverter() is not null || property.GetColumnType() is not null)
        {
            return;
        }

        if (property.ClrType.UnwrapNullableType() == typeof(DateOnly))
        {
            propertyBuilder.HasColumnType("date");
            propertyBuilder.HasConversion(
                new ValueConverter<DateOnly, DateTime>(
                    date => date.ToDateTime(TimeOnly.MinValue),
                    value => DateOnly.FromDateTime(value)));
        }
        else if (property.ClrType.UnwrapNullableType() == typeof(TimeOnly))
        {
            propertyBuilder.HasColumnType("time");
            propertyBuilder.HasConversion(
                new ValueConverter<TimeOnly, TimeSpan>(
                    time => time.ToTimeSpan(),
                    value => TimeOnly.FromTimeSpan(value)));
        }
    }
}
