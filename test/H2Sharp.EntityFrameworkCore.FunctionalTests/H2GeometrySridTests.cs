using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Storage;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;

namespace H2Sharp.EntityFrameworkCore;

public class H2GeometrySridTests
{
    [Fact]
    public void Geometry_envelope_uses_the_h2_aggregate_on_the_reference()
    {
        using var connection = new H2Connection("jdbc:h2:mem:geometry_envelope");
        using var context = new GeometryContext(new DbContextOptionsBuilder<GeometryContext>()
            .UseH2(connection, h2 => h2.UseNetTopologySuite()).Options);
        var query = context.Geometries.GroupBy(g => g.Group).Select(g => new
        {
            Group = g.Key,
            Bounds = EnvelopeCombiner.CombineAsGeometry(g.Select(row => row.Point))
        });
        var sql = query.ToQueryString();
        Assert.Contains("ENVELOPE(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ST_", sql, StringComparison.OrdinalIgnoreCase);
        if (H2ReferenceOracle.IsAvailable)
        {
            var reference = H2ReferenceOracle.Run($"""
                {context.Database.GenerateCreateScript()}
                INSERT INTO "Geometries" VALUES (1, 1, GEOMETRY 'POINT (1 2)'), (2, 1, GEOMETRY 'POINT (3 4)');
                SELECT 1 / CASE WHEN q."Bounds" = GEOMETRY 'POLYGON ((1 2, 1 4, 3 4, 3 2, 1 2))' THEN 1 ELSE 0 END FROM ({sql}) q;
                """);
            Assert.True(reference.Accepted, reference.Output);
        }
    }

    [Theory]
    [InlineData("GEOMETRY(POINT, 4326)", 4326)]
    [InlineData("geometry(point z, -1)", -1)]
    [InlineData("geometry(pointzm)", -1)]
    public void Geometry_types_keep_their_reference_system(string storeType, int expectedSrid)
    {
        Assert.True(H2NetTopologySuiteTypeMappingSourcePlugin.TryParseStoreTypeName(
            storeType, out _, out var clrType, out var srid, out _));
        Assert.Equal(typeof(Point), clrType);
        Assert.Equal(expectedSrid, srid);

        var plugin = new H2NetTopologySuiteTypeMappingSourcePlugin();
        var mapping = plugin.FindMapping(new RelationalTypeMappingInfo(storeTypeName: storeType));
        Assert.NotNull(mapping);
        Assert.Equal(storeType, mapping.StoreType);
    }

    [Fact]
    public void Geometry_literals_preserve_srid_on_the_reference()
    {
        var mapping = new H2GeometryTypeMapping<Point>("geometry(point, 4326)");
        var literal = mapping.GenerateSqlLiteral(new Point(1, 2) { SRID = 4326 });
        Assert.Equal("GEOMETRY 'SRID=4326;POINT (1 2)'", literal);

        if (H2ReferenceOracle.IsAvailable)
        {
            var reference = H2ReferenceOracle.Run($"""
                CREATE TABLE T (P GEOMETRY(POINT, 4326));
                INSERT INTO T VALUES ({literal});
                SELECT 1 / CASE WHEN CAST(P AS VARCHAR) = 'SRID=4326;POINT (1 2)' THEN 1 ELSE 0 END FROM T;
                """);
            Assert.True(reference.Accepted, reference.Output);
        }
    }

    private sealed class GeometryRow
    {
        public int Id { get; set; }
        public int Group { get; set; }
        public Point Point { get; set; } = new(0, 0);
    }

    private sealed class GeometryContext(DbContextOptions<GeometryContext> options) : DbContext(options)
    {
        public DbSet<GeometryRow> Geometries => Set<GeometryRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GeometryRow>().ToTable("Geometries");
            modelBuilder.Entity<GeometryRow>().Property(g => g.Id).ValueGeneratedNever();
        }
    }
}
