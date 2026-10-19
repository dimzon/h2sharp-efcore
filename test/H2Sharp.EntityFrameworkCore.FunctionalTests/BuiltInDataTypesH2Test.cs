using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.NetworkInformation;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class BuiltInDataTypesH2Test : BuiltInDataTypesTestBase<BuiltInDataTypesH2Test.BuiltInDataTypesH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public BuiltInDataTypesH2Test(BuiltInDataTypesH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }







    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local



    // ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local












    public class BuiltInDataTypesH2Fixture : BuiltInDataTypesFixtureBase
    {
        public override bool StrictEquality
            => false;

        public override bool SupportsAnsi
            => false;

        public override bool SupportsUnicodeToAnsiConversion
            => false;

        public override bool SupportsLargeStringComparisons
            => true;

        public override bool SupportsDecimalComparisons
            => true;

        public override bool PreservesDateTimeKind
            => false;

        // We instruct the test store to pass a connection string to UseH2() instead of a DbConnection - that's required to allow
        // EF's MapEnum() to function properly and instantiate an H2DataSource internally.
        protected override ITestStoreFactory TestStoreFactory
            => new H2TestStoreFactory(useConnectionString: true);


        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ServiceProvider.GetRequiredService<ILoggerFactory>();



        public override bool SupportsBinaryKeys
            => true;

        public override DateTime DefaultDateTime
            => new();
    }

    protected enum StringEnum16 : short
    {
        // ReSharper disable once UnusedMember.Global
        Value1 = 1,

        // ReSharper disable once UnusedMember.Global
        Value2 = 2,
        Value4 = 4
    }

    protected enum StringEnumU16 : ushort
    {
        // ReSharper disable once UnusedMember.Global
        Value1 = 1,

        // ReSharper disable once UnusedMember.Global
        Value2 = 2,
        Value4 = 4
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected class MappedDataTypes
    {
        [Column(TypeName = "int")]
        public int Int { get; set; }

        [Column(TypeName = "bigint")]
        public long LongAsBigint { get; set; }

        [Column(TypeName = "smallint")]
        public short ShortAsSmallint { get; set; }

        [Column(TypeName = "smallint")]
        public byte ByteAsSmallint { get; set; }

        [Column(TypeName = "int")]
        public uint UintAsInt { get; set; }

        [Column(TypeName = "bigint")]
        public uint UintAsBigint { get; set; }

        [Column(TypeName = "bigint")]
        public ulong UlongAsBigint { get; set; }

        [Column(TypeName = "smallint")]
        public ushort UShortAsSmallint { get; set; }

        [Column(TypeName = "int")]
        public ushort UShortAsInt { get; set; }

        //[Column(TypeName = "tinyint")]
        //public sbyte SByteAsTinyint { get; set; }

        [Column(TypeName = "boolean")]
        public bool BoolAsBoolean { get; set; }

        [Column(TypeName = "numeric")]
        public decimal Decimal { get; set; } // decimal is just an alias for numeric

        [Column(TypeName = "numeric")]
        public decimal DecimalAsNumeric { get; set; }

        [Column(TypeName = "money")]
        public decimal DecimalAsMoney { get; set; }

        [Column(TypeName = "double precision")]
        public double DoubleAsDoublePrecision { get; set; }

        [Column(TypeName = "real")]
        public float FloatAsReal { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime DateTimeAsTimestamp { get; set; }

        [Column(TypeName = "timestamptz")]
        public DateTime DateTimeAsTimestamptz { get; set; }

        [Column(TypeName = "date")]
        public DateTime DateTimeAsDate { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan TimeSpanAsTime { get; set; }

        [Column(TypeName = "timetz")]
        public DateTimeOffset DateTimeOffsetAsTimetz { get; set; }

        [Column(TypeName = "interval")]
        public TimeSpan TimeSpanAsInterval { get; set; }

        [Column(TypeName = "text")]
        public string StringAsText { get; set; }

        [Column(TypeName = "varchar")]
        public string StringAsVarchar { get; set; }

        // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
        // [Column(TypeName = "char(1)")]
        // public char? CharAsChar1 { get; set; }

        [Column(TypeName = "text")]
        public char? CharAsText { get; set; }

        [Column(TypeName = "varchar")]
        public char? CharAsVarchar { get; set; }

        [Column(TypeName = "bytea")]
        public byte[] BytesAsBytea { get; set; }

        [Column(TypeName = "uuid")]
        public Guid GuidAsUuid { get; set; }

        [Column(TypeName = "text")]
        public StringEnum16 EnumAsText { get; set; }

        [Column(TypeName = "varchar")]
        public StringEnumU16 EnumAsVarchar { get; set; }

        // PostgreSQL-specific types from here

        [Column(TypeName = "macaddr")]
        public PhysicalAddress PhysicalAddressAsMacaddr { get; set; }

        [Column(TypeName = "jsonb")]
        public string StringAsJsonb { get; set; }

        [Column(TypeName = "json")]
        public string StringAsJson { get; set; }

        [Column(TypeName = "hstore")]
        public Dictionary<string, string> DictionaryAsHstore { get; set; }

        [Column(TypeName = "int[]")]
        public int[] IntArrayAsIntArray { get; set; }

        [Column(TypeName = "macaddr[]")]
        public PhysicalAddress[] PhysicalAddressArrayAsMacaddrArray { get; set; }

        [Column(TypeName = "xid")]
        public uint UintAsXid { get; set; }

        public uint Regconfig { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        [Column(TypeName = "mood")]
        public Mood Mood { get; set; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class MappedSizedDataTypes
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Id { get; set; }
        /*
        public string Char { get; set; }
        public string Character { get; set; }
        public string Varchar { get; set; }
        public string Char_varying { get; set; }
        public string Character_varying { get; set; }
        public string Nchar { get; set; }
        public string National_character { get; set; }
        public string Nvarchar { get; set; }
        public string National_char_varying { get; set; }
        public string National_character_varying { get; set; }
        public byte[] Binary { get; set; }
        public byte[] Varbinary { get; set; }
        public byte[] Binary_varying { get; set; }
        */
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class MappedScaledDataTypes
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Id { get; set; }
        /*
        public float Float { get; set; }
        public float Double_precision { get; set; }
        public DateTimeOffset Datetimeoffset { get; set; }
        public DateTime Datetime2 { get; set; }
        public decimal Decimal { get; set; }
        public decimal Dec { get; set; }
        public decimal Numeric { get; set; }
        */
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public class MappedPrecisionAndScaledDataTypes
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public int Id { get; set; }
        /*
        public decimal Decimal { get; set; }
        public decimal Dec { get; set; }
        public decimal Numeric { get; set; }
        */
    }

    // ReSharper disable once MemberCanBePrivate.Global
    protected class MappedNullableDataTypes
    {
        [Column(TypeName = "int")]
        public int? Int { get; set; }

        [Column(TypeName = "bigint")]
        public long? LongAsBigint { get; set; }

        [Column(TypeName = "smallint")]
        public short? ShortAsSmallint { get; set; }

        [Column(TypeName = "smallint")]
        public byte? ByteAsSmallint { get; set; }

        [Column(TypeName = "int")]
        public uint? UintAsInt { get; set; }

        [Column(TypeName = "bigint")]
        public uint? UintAsBigint { get; set; }

        [Column(TypeName = "bigint")]
        public ulong? UlongAsBigint { get; set; }

        [Column(TypeName = "smallint")]
        public ushort? UShortAsSmallint { get; set; }

        [Column(TypeName = "int")]
        public ushort? UShortAsInt { get; set; }

        //[Column(TypeName = "tinyint")]
        //public sbyte? SByteAsTinyint { get; set; }

        [Column(TypeName = "boolean")]
        public bool? BoolAsBoolean { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? Decimal { get; set; } // decimal is just an alias for numeric

        [Column(TypeName = "numeric")]
        public decimal? DecimalAsNumeric { get; set; }

        [Column(TypeName = "money")]
        public decimal? DecimalAsMoney { get; set; }

        [Column(TypeName = "numeric")]
        public decimal? BigIntegerAsNumeric { get; set; }

        [Column(TypeName = "double precision")]
        public double? DoubleAsDoublePrecision { get; set; }

        [Column(TypeName = "real")]
        public float? FloatAsReal { get; set; }

        [Column(TypeName = "timestamp")]
        public DateTime? DateTimeAsTimestamp { get; set; }

        [Column(TypeName = "timestamptz")]
        public DateTime? DateTimeAsTimestamptz { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateTimeAsDate { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? TimeSpanAsTime { get; set; }

        [Column(TypeName = "date")]
        public DateOnly? DateOnlyAsDate { get; set; }

        [Column(TypeName = "time")]
        public TimeOnly? TimeOnlyAsTime { get; set; }

        [Column(TypeName = "timetz")]
        public DateTimeOffset? DateTimeOffsetAsTimetz { get; set; }

        [Column(TypeName = "interval")]
        public TimeSpan? TimeSpanAsInterval { get; set; }

        [Column(TypeName = "text")]
        public string StringAsText { get; set; }

        [Column(TypeName = "varchar")]
        public string StringAsVarchar { get; set; }

        // TODO: enable here (and above) after https://github.com/aspnet/EntityFrameworkCore/issues/14159 is fixed
        // [Column(TypeName = "char(1)")]
        // public char? CharAsChar1 { get; set; }

        [Column(TypeName = "text")]
        public char? CharAsText { get; set; }

        [Column(TypeName = "varchar")]
        public char? CharAsVarchar { get; set; }

        [Column(TypeName = "bytea")]
        public byte[] BytesAsBytea { get; set; }

        [Column(TypeName = "uuid")]
        public Guid? GuidAsUuid { get; set; }

        [Column(TypeName = "text")]
        public StringEnum16? EnumAsText { get; set; }

        [Column(TypeName = "varchar")]
        public StringEnumU16? EnumAsVarchar { get; set; }

        // PostgreSQL-specific types from here

        [Column(TypeName = "macaddr")]
        public PhysicalAddress PhysicalAddressAsMacaddr { get; set; }

        [Column(TypeName = "jsonb")]
        public string StringAsJsonb { get; set; }

        [Column(TypeName = "json")]
        public string StringAsJson { get; set; }

        [Column(TypeName = "hstore")]
        public Dictionary<string, string> DictionaryAsHstore { get; set; }

        [Column(TypeName = "hstore")]
        public ImmutableDictionary<string, string> ImmutableDictionaryAsHstore { get; set; }

        [Column(TypeName = "int[]")]
        public int[] IntArrayAsIntArray { get; set; }

        [Column(TypeName = "macaddr[]")]
        public PhysicalAddress[] PhysicalAddressArrayAsMacaddrArray { get; set; }

        [Column(TypeName = "xid")]
        public uint? UintAsXid { get; set; }

        public uint? Regconfig { get; set; }

        [Column(TypeName = "mood")]
        public Mood? Mood { get; set; }
    }
}

// ReSharper disable once UnusedMember.Global
public enum Mood { Happy, Sad }
