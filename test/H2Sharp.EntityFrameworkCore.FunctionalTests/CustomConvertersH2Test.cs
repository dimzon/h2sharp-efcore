using System.Globalization;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class CustomConvertersH2Test(CustomConvertersH2Test.CustomConvertersH2Fixture fixture)
    : CustomConvertersTestBase<CustomConvertersH2Test.CustomConvertersH2Fixture>(fixture)
{
    // Disabled: PostgreSQL is case-sensitive
    public override Task Can_insert_and_read_back_with_case_insensitive_string_key()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "DateTimeOffset with non-zero offset, https://github.com/dotnet/efcore/issues/26068")]
    public override Task Can_insert_and_read_back_non_nullable_backed_data_types()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "DateTimeOffset with non-zero offset, https://github.com/dotnet/efcore/issues/26068")]
    public override Task Can_insert_and_read_back_nullable_backed_data_types()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "DateTimeOffset with non-zero offset, https://github.com/dotnet/efcore/issues/26068")]
    public override Task Can_insert_and_read_back_object_backed_data_types()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "DateTimeOffset with non-zero offset, https://github.com/dotnet/efcore/issues/26068")]
    public override Task Can_query_using_any_data_type_nullable_shadow()
        => Task.CompletedTask;

    public override void Value_conversion_on_enum_collection_contains()
        => Assert.Contains(
            CoreStrings.TranslationFailed("").Substring(47),
            Assert.Throws<InvalidOperationException>(() => base.Value_conversion_on_enum_collection_contains()).Message);

    public class CustomConvertersH2Fixture : CustomConvertersFixtureBase
    {
        public override bool StrictEquality
            => true;

        public override bool SupportsAnsi
            => false;

        public override bool SupportsUnicodeToAnsiConversion
            => true;

        public override bool SupportsLargeStringComparisons
            => true;

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public override bool SupportsBinaryKeys
            => true;

        public override bool SupportsDecimalComparisons
            => true;

        public override DateTime DefaultDateTime
            => new();

        public override bool PreservesDateTimeKind
            => false;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // The shared model converts DateOnly through ToShortDateString/Parse, which is
            // culture-dependent; H2's DATE literal understands only the ISO form. Keep the
            // same value converter shape but write and parse the invariant date.
            modelBuilder.Entity<BuiltInDataTypes>().Property(e => e.TestDateOnly)
                .HasConversion(
                    v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    v => DateOnly.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            modelBuilder.Entity<BuiltInDataTypesShadow>().Property(nameof(BuiltInDataTypes.TestDateOnly))
                .HasConversion(new ValueConverter<DateOnly, string>(
                    v => v.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    v => DateOnly.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture)));
            modelBuilder.Entity<BuiltInNullableDataTypes>().Property(e => e.TestNullableDateOnly)
                .HasConversion(
                    v => v.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    v => DateOnly.ParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture));
            modelBuilder.Entity<BuiltInDataTypes>().Property(e => e.TestTimeOnly)
                .HasConversion(
                    v => v.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
                    v => TimeOnly.Parse(v, CultureInfo.InvariantCulture));
            modelBuilder.Entity<BuiltInDataTypesShadow>().Property(nameof(BuiltInDataTypes.TestTimeOnly))
                .HasConversion(new ValueConverter<TimeOnly, string>(
                    v => v.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
                    v => TimeOnly.Parse(v, CultureInfo.InvariantCulture)));
            modelBuilder.Entity<BuiltInNullableDataTypes>().Property(e => e.TestNullableTimeOnly)
                .HasConversion(
                    v => v.Value.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
                    v => TimeOnly.Parse(v, CultureInfo.InvariantCulture));

            // We default to mapping DateTime to 'timestamp with time zone', but the seeding data has Unspecified DateTimes which aren't
            // supported.
            modelBuilder.Entity<ObjectBackedDataTypes>().Property(b => b.DateTime)
                .HasColumnType("timestamp without time zone");
            modelBuilder.Entity<NullableBackedDataTypes>().Property(b => b.DateTime)
                .HasColumnType("timestamp without time zone");
            modelBuilder.Entity<NonNullableBackedDataTypes>().Property(b => b.DateTime)
                .HasColumnType("timestamp without time zone");

            // We don't support DateTimeOffset with non-zero offset, so we need to override the seeding data
            var objectBackedDataTypes = modelBuilder.Entity<ObjectBackedDataTypes>().Metadata.GetSeedData().Single();
            objectBackedDataTypes[nameof(ObjectBackedDataTypes.DateTimeOffset)]
                = new DateTimeOffset(new DateTime(), TimeSpan.Zero);

            var nullableBackedDataTypes = modelBuilder.Entity<NullableBackedDataTypes>().Metadata.GetSeedData().Single();
            nullableBackedDataTypes[nameof(NullableBackedDataTypes.DateTimeOffset)]
                = new DateTimeOffset(DateTime.Parse("01/01/2000 12:34:56"), TimeSpan.Zero);

            var nonNullableBackedDataTypes = modelBuilder.Entity<NonNullableBackedDataTypes>().Metadata.GetSeedData().Single();
            nonNullableBackedDataTypes[nameof(NonNullableBackedDataTypes.DateTimeOffset)]
                = new DateTimeOffset(new DateTime(), TimeSpan.Zero);
        }
    }
}
