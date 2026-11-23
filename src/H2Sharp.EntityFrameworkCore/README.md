# H2Sharp.EntityFrameworkCore

An Entity Framework Core provider for [H2Sharp](https://github.com/dimzon/h2sharp) — the C# port of
H2 Database 2.5. It is a fork of EFCore.PG with PostgreSQL's surface replaced by H2's: the type
mappings, migrations SQL, connection handling and DDL are H2's. The provider is built for
.NET 8 and .NET 10.

```csharp
await using var ctx = new BlogContext();
await ctx.Database.EnsureDeletedAsync();
await ctx.Database.EnsureCreatedAsync();

ctx.Blogs.Add(new() { Name = "FooBlog" });
await ctx.SaveChangesAsync();

var fBlogs = await ctx.Blogs.Where(b => b.Name.StartsWith("F")).ToListAsync();

public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseH2("Data Source=mydatabase;Storage=Memory");
}

public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

An H2 database belongs to a **data source**: the data source creates it, keeps it alive and hands
out the connections. The connection string names the database and `Storage` says whether it is
`Memory` or `File`. A JDBC URL is not a connection string here. To own the database yourself,
pass an `H2DataSource` instance to `UseH2`.

`EF.Functions` exposes the built-in functions of H2 that the engine can run: date and time,
string, binary, number and bit functions. Arrays, JSON, window functions and `INFORMATION_SCHEMA`
are mapped as H2 2.5 defines them.

The provider is under active development. Tests that fail because the engine does not yet read a
documented H2 construct are marked `BlockedByPort` and listed in
[DIVERGENCES.md](https://github.com/dimzon/h2sharp-efcore/blob/h2-provider/docs/DIVERGENCES.md).

Documentation: [usage guide](https://github.com/dimzon/h2sharp-efcore/blob/h2-provider/docs/USAGE.md),
[SQL dialect](https://github.com/dimzon/h2sharp-efcore/blob/h2-provider/docs/DIALECT.md),
[repository](https://github.com/dimzon/h2sharp-efcore).

Licensed under the PostgreSQL License, inherited from EFCore.PG.
