# H2Sharp Entity Framework Core — NetTopologySuite

This plugin maps NetTopologySuite geometry types to H2 2.5 `GEOMETRY` columns.
H2 supports shape and dimension constraints, optional SRID constraints, and WKT/EWKT
literals, including `GEOMETRY 'SRID=4326;POINT (1 2)'`.

Enable the mappings when configuring your context:

```csharp
optionsBuilder.UseH2("jdbc:h2:mem:cities", h2 => h2.UseNetTopologySuite());
```

For a constrained point column:

```csharp
modelBuilder.Entity<City>()
    .Property(city => city.Center)
    .HasColumnType("geometry(point, 4326)");
```

`EnvelopeCombiner.CombineAsGeometry` over a group translates to H2's `ENVELOPE`
aggregate, which returns the minimum bounding box in the 2D coordinate plane.

H2 has no PostGIS `ST_*` built-ins, `GEOGRAPHY` type or KNN `<->` operator.
NetTopologySuite computations without an H2 SQL equivalent can be performed after
materializing the geometries. Geometry equality, bounding-box overlap and topological
intersection have different semantics and are not substituted for one another.

The provider targets H2 2.5. Port-specific implementation gaps are tracked in
[`docs/DIVERGENCES.md`](../../docs/DIVERGENCES.md).
