using H2Sharp.EntityFrameworkCore.TestUtilities;
using Xunit.Sdk;

namespace H2Sharp.EntityFrameworkCore.Query;

/// <summary>
///     The H2 built-in functions the provider offers on <see cref="DbFunctions" />, checked two ways:
///     the SQL the provider builds, and the answer the engine gives back.
/// </summary>
public class H2DbFunctionsTest
{
    private static readonly IServiceProvider ServiceProvider = new ServiceCollection()
        .AddEntityFrameworkH2()
        .BuildServiceProvider();

    private sealed class FunctionsContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Row> Rows { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Row>(b =>
            {
                b.Property(e => e.Id).ValueGeneratedNever();
                b.Property(e => e.Name).HasColumnType("varchar(50)");
                b.Property(e => e.Price).HasColumnType("decimal(10,2)");
                b.Property(e => e.Stamp).HasColumnType("timestamp");
            });
        }
    }

    private sealed class Row
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public DateTime Stamp { get; set; }
    }

    private static FunctionsContext CreateContext(string databaseName)
        => new(new DbContextOptionsBuilder<FunctionsContext>()
            .UseH2($"Data Source=mem:{databaseName}")
            .UseInternalServiceProvider(ServiceProvider)
            .Options);

    /// <summary>
    ///     The SQL the provider builds for a query that uses the given function.
    /// </summary>
    private static string SqlOf<T>(string databaseName, Func<DbFunctions, IQueryable<Row>, IQueryable<T>> query)
    {
        using var context = CreateContext(databaseName);

        return query(EF.Functions, context.Rows)
            .ToQueryString();
    }

    [Fact]
    public void Date_functions_are_translated_to_the_H2_built_ins()
    {
        var sql = SqlOf(
            "functions-date",
            (f, rows) => rows.Select(r => new
            {
                Added = f.DateAdd("DAY", 1, r.Stamp),
                Difference = f.DateDiff("DAY", r.Stamp, r.Stamp),
                Truncated = f.DateTrunc("MONTH", r.Stamp),
                Day = f.Extract("DAY", r.Stamp),
                DayName = f.DayName(r.Stamp),
                MonthName = f.MonthName(r.Stamp)
            }));

        Assert.Contains("DATEADD('DAY', 1,", sql);
        Assert.Contains("DATEDIFF('DAY',", sql);
        Assert.Contains("DATE_TRUNC('MONTH',", sql);
        Assert.Contains("DAY(", sql);
        Assert.Contains("DAYNAME(", sql);
        Assert.Contains("MONTHNAME(", sql);
    }

    [Fact]
    public void String_functions_are_translated_to_the_H2_built_ins()
    {
        var sql = SqlOf(
            "functions-string",
            (f, rows) => rows.Select(r => new
            {
                Position = f.Locate(r.Name!, "a"),
                Padded = f.LeftPad(r.Name!, 10),
                PaddedRight = f.RightPad(r.Name!, 10),
                Translated = f.Translate(r.Name!, "a", "b"),
                Repeated = f.Repeat(r.Name!, 2),
                Spaces = f.Space(3),
                Quoted = f.QuoteIdentifier(r.Name!),
                Soundex = f.Soundex(r.Name!),
                Difference = f.SoundexDifference(r.Name!, r.Name!),
                Inserted = f.Insert(r.Name!, 1, 2, "x"),
                Code = f.Ascii(r.Name!),
                Character = f.Char(65),
                Decoded = f.StringDecode(r.Name!),
                Encoded = f.StringEncode(r.Name!)
            }));

        Assert.Contains("LOCATE(", sql);
        Assert.Contains("LPAD(", sql);
        Assert.Contains("RPAD(", sql);
        Assert.Contains("TRANSLATE(", sql);
        Assert.Contains("REPEAT(", sql);
        Assert.Contains("SPACE(", sql);
        Assert.Contains("QUOTE_IDENT(", sql);
        Assert.Contains("SOUNDEX(", sql);
        Assert.Contains("DIFFERENCE(", sql);
        Assert.Contains("INSERT(", sql);
        Assert.Contains("ASCII(", sql);
        Assert.Contains("CHAR(", sql);
        Assert.Contains("STRINGDECODE(", sql);
        Assert.Contains("STRINGENCODE(", sql);
    }

    [Fact]
    public void Math_and_bit_functions_are_translated_to_the_H2_built_ins()
    {
        var sql = SqlOf(
            "functions-math",
            (f, rows) => rows.Select(r => new
            {
                Sign = f.Sign(r.Price),
                Truncated = f.Truncate(r.Price, 1),
                Whole = f.Truncate(r.Price),
                And = f.BitAnd(1, 2),
                Or = f.BitOr(1, 2),
                Xor = f.BitXor(1, 2),
                Not = f.BitNot(1),
                Nand = f.BitNand(1, 2),
                Nor = f.BitNor(1, 2),
                Xnor = f.BitXnor(1, 2),
                Get = f.BitGet(1, 1),
                Count = f.BitCount(1),
                Left = f.LeftShift(1, 2),
                Right = f.RightShift(4, 1),
                UnsignedLeft = f.UnsignedLeftShift(1, 2),
                UnsignedRight = f.UnsignedRightShift(4, 1),
                RotateLeft = f.RotateLeft(1, 1),
                RotateRight = f.RotateRight(1, 1),
                Cotangent = f.Cotangent(1.0),
                Rounded = f.RoundMagic(r.Price)
            }));

        Assert.Contains("SIGN(", sql);
        Assert.Contains("TRUNCATE(", sql);
        Assert.Contains("TRUNC(", sql);
        Assert.Contains("BITAND(", sql);
        Assert.Contains("BITOR(", sql);
        Assert.Contains("BITXOR(", sql);
        Assert.Contains("BITNOT(", sql);
        Assert.Contains("BITNAND(", sql);
        Assert.Contains("BITNOR(", sql);
        Assert.Contains("BITXNOR(", sql);
        Assert.Contains("BITGET(", sql);
        Assert.Contains("BITCOUNT(", sql);
        Assert.Contains("LSHIFT(", sql);
        Assert.Contains("RSHIFT(", sql);
        Assert.Contains("ULSHIFT(", sql);
        Assert.Contains("URSHIFT(", sql);
        Assert.Contains("ROTATELEFT(", sql);
        Assert.Contains("ROTATERIGHT(", sql);
        Assert.Contains("COT(", sql);
        Assert.Contains("ROUNDMAGIC(", sql);
    }

    [Fact]
    public void Binary_functions_are_translated_to_the_H2_built_ins()
    {
        var sql = SqlOf(
            "functions-binary",
            (f, rows) => rows.Select(r => new
            {
                Utf8 = f.StringToUtf8(r.Name!),
                Text = f.Utf8ToString(f.StringToUtf8(r.Name!)),
                Compressed = f.Compress(r.Name!)
            }));

        Assert.Contains("STRINGTOUTF8(", sql);
        Assert.Contains("UTF8TOSTRING(", sql);
        Assert.Contains("COMPRESS(", sql);
    }

    [Fact]
    public void Date_arithmetic_is_translated_to_TIMESTAMPADD()
    {
        var sql = SqlOf(
            "functions-arithmetic",
            (f, rows) => rows.Select(r => new
            {
                Later = r.Stamp.AddDays(1),
                MuchLater = r.Stamp.AddMonths(2),
                Earlier = r.Stamp.AddYears(-1)
            }));

        // H2 has no interval literal, so the arithmetic cannot be "timestamp + interval".
        Assert.Contains("TIMESTAMPADD('DAY',", sql);
        Assert.Contains("TIMESTAMPADD('MONTH',", sql);
        Assert.Contains("TIMESTAMPADD('YEAR',", sql);
        Assert.DoesNotContain("INTERVAL", sql);
    }

    [Fact]
    public void The_engine_answers_the_functions_the_provider_builds()
    {
        var databaseName = "functions-engine";

        using var context = CreateContext(databaseName);
        context.Database.OpenConnection();

        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE "Rows" (
                "Id" integer NOT NULL,
                "Name" varchar(50),
                "Price" decimal(10,2) NOT NULL,
                "Stamp" timestamp,
                CONSTRAINT "PK_Rows" PRIMARY KEY ("Id")
            )
            """);

        context.Rows.Add(
            new Row
            {
                Id = 1,
                Name = "abcdef",
                Price = 12.345m,
                Stamp = new DateTime(2020, 5, 6, 7, 8, 9, DateTimeKind.Unspecified)
            });

        context.SaveChanges();

        // The queries go straight to the connection rather than through EF's own pipeline: the
        // engine's parser does not read the "AS" in front of a table alias yet, and every query
        // EF builds has one. The SQL the provider builds is checked by the tests above.
        Assert.Equal(3, Scalar<int>("LOCATE('c', 'abcdef')"));
        Assert.Equal(0, Scalar<int>("LOCATE('z', 'abcdef')"));
        Assert.Equal("000abcdef", Scalar<string>("LPAD('abcdef', 9, '0')"));
        Assert.Equal("abcdef000", Scalar<string>("RPAD('abcdef', 9, '0')"));
        Assert.Equal("xbcdyf", Scalar<string>("TRANSLATE('abcdef', 'ae', 'xy')"));
        Assert.Equal("abcdefabcdef", Scalar<string>("REPEAT('abcdef', 2)"));
        Assert.Equal("   ", Scalar<string>("SPACE(3)"));
        Assert.Equal("\"abcdef\"", Scalar<string>("QUOTE_IDENT('abcdef')"));
        Assert.Equal(97, Scalar<int>("ASCII('abcdef')"));
        Assert.Equal("A", Scalar<string>("CHAR(65)"));
        Assert.Equal(-1, Scalar<int>("SIGN(-1)"));
        Assert.Equal(12.3m, Scalar<decimal>("TRUNCATE(12.345, 1)"));
        Assert.Equal(12m, Scalar<decimal>("TRUNC(12.345)"));
        Assert.Equal(8, Scalar<int>("BITAND(12, 10)"));
        Assert.Equal(14, Scalar<int>("BITOR(12, 10)"));
        Assert.Equal(6, Scalar<int>("BITXOR(12, 10)"));
        Assert.Equal(2, Scalar<int>("BITCOUNT(12)"));
        Assert.Equal(8, Scalar<int>("LSHIFT(1, 3)"));
        Assert.Equal(2, Scalar<int>("RSHIFT(8, 2)"));
        Assert.Equal(4, Scalar<int>("ROTATELEFT(1, 2)"));
        Assert.Equal(2, Scalar<int>("ROTATERIGHT(4, 1)"));
        Assert.Equal("abc", Scalar<string>("UTF8TOSTRING(STRINGTOUTF8('abc'))"));
        Assert.Equal("R163", Scalar<string>("SOUNDEX('Robert')"));
        Assert.Equal(4, Scalar<int>("DIFFERENCE('Robert', 'Rupert')"));

        // The engine's parser does not read a timestamp literal, so the dates come from the row.
        var stamp = "\"Stamp\"";
        Assert.Equal(7, Scalar<int>($"DAY(DATEADD('DAY', 1, {stamp})) FROM \"Rows\""));
        Assert.Equal(1, Scalar<int>($"DATEDIFF('DAY', {stamp}, DATEADD('DAY', 1, {stamp})) FROM \"Rows\""));
        Assert.Equal(1, Scalar<int>($"DAY(DATE_TRUNC('MONTH', {stamp})) FROM \"Rows\""));
        Assert.Equal(6, Scalar<int>($"DAY({stamp}) FROM \"Rows\""));
        Assert.Equal("Wednesday", Scalar<string>($"DAYNAME({stamp}) FROM \"Rows\""));
        Assert.Equal("May", Scalar<string>($"MONTHNAME({stamp}) FROM \"Rows\""));

        context.Database.EnsureDeleted();

        T Scalar<T>(string expression)
        {
            using var command = ((H2Connection)context.Database.GetService<IRelationalConnection>().DbConnection).CreateCommand();
            command.CommandText = $"SELECT {expression}";
            // The engine answers with the widest type it has; the test asks for the one it wants.
            return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
        }
    }
}
