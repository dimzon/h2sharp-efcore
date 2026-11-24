using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class ValueConvertersEndToEndH2Test(ValueConvertersEndToEndH2Test.ValueConvertersEndToEndH2Fixture fixture)
    : ValueConvertersEndToEndTestBase<ValueConvertersEndToEndH2Test.ValueConvertersEndToEndH2Fixture>(fixture)
{
    [ConditionalTheory(Skip = "DateTime and DateTimeOffset, https://github.com/dotnet/efcore/issues/26068")]
    public override Task Can_insert_and_read_back_with_conversions(int[] valueOrder)
        => base.Can_insert_and_read_back_with_conversions(valueOrder);

    [ConditionalTheory]
    [InlineData(nameof(ConvertingEntity.BoolAsChar), "character(1)", false)]
    [InlineData(nameof(ConvertingEntity.BoolAsNullableChar), "character(1)", false)]
    [InlineData(nameof(ConvertingEntity.BoolAsString), "varchar(3)", false)]
    [InlineData(nameof(ConvertingEntity.BoolAsInt), "integer", false)]
    [InlineData(nameof(ConvertingEntity.BoolAsNullableString), "varchar(3)", false)]
    [InlineData(nameof(ConvertingEntity.BoolAsNullableInt), "integer", false)]
    [InlineData(nameof(ConvertingEntity.IntAsLong), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.IntAsNullableLong), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.BytesAsString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.BytesAsNullableString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.CharAsString), "varchar(1)", false)]
    [InlineData(nameof(ConvertingEntity.CharAsNullableString), "varchar(1)", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeOffsetToBinary), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeOffsetToNullableBinary), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeOffsetToString), "varchar(48)", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeOffsetToNullableString), "varchar(48)", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeToBinary), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeToNullableBinary), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeToString), "varchar(48)", false)]
    [InlineData(nameof(ConvertingEntity.DateTimeToNullableString), "varchar(48)", false)]
    [InlineData(nameof(ConvertingEntity.EnumToString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.EnumToNullableString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.EnumToNumber), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.EnumToNullableNumber), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.GuidToString), "varchar(36)", false)]
    [InlineData(nameof(ConvertingEntity.GuidToNullableString), "varchar(36)", false)]
    [InlineData(nameof(ConvertingEntity.GuidToBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.GuidToNullableBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.IPAddressToString), "varchar(45)", false)]
    [InlineData(nameof(ConvertingEntity.IPAddressToNullableString), "varchar(45)", false)]
    [InlineData(nameof(ConvertingEntity.IPAddressToBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.IPAddressToNullableBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.PhysicalAddressToString), "varchar(20)", false)]
    [InlineData(nameof(ConvertingEntity.PhysicalAddressToNullableString), "varchar(20)", false)]
    [InlineData(nameof(ConvertingEntity.PhysicalAddressToBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.PhysicalAddressToNullableBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.NumberToString), "varchar(64)", false)]
    [InlineData(nameof(ConvertingEntity.NumberToNullableString), "varchar(64)", false)]
    [InlineData(nameof(ConvertingEntity.NumberToBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.NumberToNullableBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.StringToBool), "boolean", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableBool), "boolean", false)]
    [InlineData(nameof(ConvertingEntity.StringToBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableBytes), "varbinary", false)]
    [InlineData(nameof(ConvertingEntity.StringToChar), "character(1)", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableChar), "character(1)", false)]
    [InlineData(nameof(ConvertingEntity.StringToDateTime), "timestamp", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableDateTime), "timestamp", false)]
    // [InlineData(nameof(ConvertingEntity.StringToDateTimeOffset), "timestamp", false)]
    // [InlineData(nameof(ConvertingEntity.StringToNullableDateTimeOffset), "timestamp", false)]
    [InlineData(nameof(ConvertingEntity.StringToEnum), "integer", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableEnum), "integer", false)]
    [InlineData(nameof(ConvertingEntity.StringToGuid), "uuid", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableGuid), "uuid", false)]
    [InlineData(nameof(ConvertingEntity.StringToNumber), "smallint", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableNumber), "smallint", false)]
    [InlineData(nameof(ConvertingEntity.StringToTimeSpan), "interval day to second", false)]
    [InlineData(nameof(ConvertingEntity.StringToNullableTimeSpan), "interval day to second", false)]
    [InlineData(nameof(ConvertingEntity.TimeSpanToTicks), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.TimeSpanToNullableTicks), "bigint", false)]
    [InlineData(nameof(ConvertingEntity.TimeSpanToString), "varchar(48)", false)]
    [InlineData(nameof(ConvertingEntity.TimeSpanToNullableString), "varchar(48)", false)]
    [InlineData(nameof(ConvertingEntity.UriToString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.UriToNullableString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.NullableCharAsString), "varchar(1)", true)]
    [InlineData(nameof(ConvertingEntity.NullableCharAsNullableString), "varchar(1)", true)]
    [InlineData(nameof(ConvertingEntity.NullableBoolAsChar), "character(1)", true)]
    [InlineData(nameof(ConvertingEntity.NullableBoolAsNullableChar), "character(1)", true)]
    [InlineData(nameof(ConvertingEntity.NullableBoolAsString), "varchar(3)", true)]
    [InlineData(nameof(ConvertingEntity.NullableBoolAsNullableString), "varchar(3)", true)]
    [InlineData(nameof(ConvertingEntity.NullableBoolAsInt), "integer", true)]
    [InlineData(nameof(ConvertingEntity.NullableBoolAsNullableInt), "integer", true)]
    [InlineData(nameof(ConvertingEntity.NullableIntAsLong), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableIntAsNullableLong), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableBytesAsString), "varchar", true)]
    [InlineData(nameof(ConvertingEntity.NullableBytesAsNullableString), "varchar", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeOffsetToBinary), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeOffsetToNullableBinary), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeOffsetToString), "varchar(48)", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeOffsetToNullableString), "varchar(48)", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeToBinary), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeToNullableBinary), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeToString), "varchar(48)", true)]
    [InlineData(nameof(ConvertingEntity.NullableDateTimeToNullableString), "varchar(48)", true)]
    [InlineData(nameof(ConvertingEntity.NullableEnumToString), "varchar", true)]
    [InlineData(nameof(ConvertingEntity.NullableEnumToNullableString), "varchar", true)]
    [InlineData(nameof(ConvertingEntity.NullableEnumToNumber), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableEnumToNullableNumber), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableGuidToString), "varchar(36)", true)]
    [InlineData(nameof(ConvertingEntity.NullableGuidToNullableString), "varchar(36)", true)]
    [InlineData(nameof(ConvertingEntity.NullableGuidToBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableGuidToNullableBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableIPAddressToString), "varchar(45)", true)]
    [InlineData(nameof(ConvertingEntity.NullableIPAddressToNullableString), "varchar(45)", true)]
    [InlineData(nameof(ConvertingEntity.NullableIPAddressToBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableIPAddressToNullableBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullablePhysicalAddressToString), "varchar(20)", true)]
    [InlineData(nameof(ConvertingEntity.NullablePhysicalAddressToNullableString), "varchar(20)", true)]
    [InlineData(nameof(ConvertingEntity.NullablePhysicalAddressToBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullablePhysicalAddressToNullableBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableNumberToString), "varchar(64)", true)]
    [InlineData(nameof(ConvertingEntity.NullableNumberToNullableString), "varchar(64)", true)]
    [InlineData(nameof(ConvertingEntity.NullableNumberToBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableNumberToNullableBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToBool), "boolean", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableBool), "boolean", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableBytes), "varbinary", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToChar), "character(1)", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableChar), "character(1)", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToDateTime), "timestamp", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableDateTime), "timestamp", true)]
    //[InlineData(nameof(ConvertingEntity.NullableStringToDateTimeOffset), "timestamp", true)]
    //[InlineData(nameof(ConvertingEntity.NullableStringToNullableDateTimeOffset), "timestamp", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToEnum), "integer", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableEnum), "integer", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToGuid), "uuid", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableGuid), "uuid", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNumber), "smallint", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableNumber), "smallint", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToTimeSpan), "interval day to second", true)]
    [InlineData(nameof(ConvertingEntity.NullableStringToNullableTimeSpan), "interval day to second", true)]
    [InlineData(nameof(ConvertingEntity.NullableTimeSpanToTicks), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableTimeSpanToNullableTicks), "bigint", true)]
    [InlineData(nameof(ConvertingEntity.NullableTimeSpanToString), "varchar(48)", true)]
    [InlineData(nameof(ConvertingEntity.NullableTimeSpanToNullableString), "varchar(48)", true)]
    [InlineData(nameof(ConvertingEntity.NullableUriToString), "varchar", true)]
    [InlineData(nameof(ConvertingEntity.NullableUriToNullableString), "varchar", true)]
    [InlineData(nameof(ConvertingEntity.NullStringToNonNullString), "varchar", false)]
    [InlineData(nameof(ConvertingEntity.NonNullStringToNullString), "varchar", true)]
    public virtual void Properties_with_conversions_map_to_appropriately_null_columns(
        string propertyName,
        string databaseType,
        bool isNullable)
    {
        using var context = CreateContext();

        var property = context.Model.FindEntityType(typeof(ConvertingEntity))!.FindProperty(propertyName);

        Assert.Equal(databaseType, property!.GetColumnType());
        Assert.Equal(isNullable, property!.IsNullable);
    }

    [ConditionalFact]
    public async Task Can_insert_and_read_back_with_value_converted_array()
    {
        await using var ctx = CreateContext();

        var entity = new ValueConvertedArrayEntity { Values = [new(8), new(9)] };
        ctx.Add(entity);
        await ctx.SaveChangesAsync();

        var id = entity.Id;
        ctx.ChangeTracker.Clear();

        entity = await ctx.Set<ValueConvertedArrayEntity>().SingleAsync(v => v.Id == id);
        Assert.Equal(8, entity.Values[0].Value);
        Assert.Equal(9, entity.Values[1].Value);
    }

    public class ValueConvertersEndToEndH2Fixture : ValueConvertersEndToEndFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<ConvertingEntity>(
                b =>
                {
                    // We map DateTimeOffset to PG 'timestamp with time zone', which doesn't store the time zone - so lossless
                    // round-tripping is impossible.
                    b.Ignore(e => e.StringToDateTimeOffset);
                    b.Ignore(e => e.StringToNullableDateTimeOffset);
                    b.Ignore(e => e.NullableStringToDateTimeOffset);
                    b.Ignore(e => e.NullableStringToNullableDateTimeOffset);
                });

            // Add some H2-specific value conversion scenarios
            modelBuilder.Entity<ValueConvertedArrayEntity>()
                .PrimitiveCollection(x => x.Values)
                .ElementType(e => e.HasConversion(typeof(IntWrapperConverter)));
        }

        private class IntWrapperConverter() : ValueConverter<IntWrapper, int>(iw => iw.Value, i => new IntWrapper(i));
    }

#nullable enable
    public class ValueConvertedArrayEntity
    {
        public int Id { get; set; }
        public IntWrapper[] Values { get; set; } = null!;
    }

    public class IntWrapper(int value) : IEquatable<IntWrapper>
    {
        public int Value { get; } = value;

        public bool Equals(IntWrapper? other)
            => other is not null && Value == other.Value;

        public override bool Equals(object? obj)
            => obj is IntWrapper other && Equals(other);

        public override int GetHashCode()
            => Value.GetHashCode();
    }
}
