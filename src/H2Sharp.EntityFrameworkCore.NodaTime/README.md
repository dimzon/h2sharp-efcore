# H2Sharp.EntityFrameworkCore.NodaTime

A plugin for the H2Sharp Entity Framework Core provider that maps
[NodaTime](https://nodatime.org) types to H2 date and time columns. It gives applications a safer
API for instants, local dates and time zones than the BCL `DateTime` types.

```csharp
using NodaTime;

await using var ctx = new BlogContext();
await ctx.Database.EnsureDeletedAsync();
await ctx.Database.EnsureCreatedAsync();

ctx.Blogs.Add(new()
{
    Name = "FooBlog",
    CreationTime = SystemClock.Instance.GetCurrentInstant()
});
await ctx.SaveChangesAsync();

var newBlogs = await ctx.Blogs
    .Where(b => b.CreationTime >= Instant.FromUtc(2020, 1, 1, 0, 0, 0))
    .ToListAsync();

public class BlogContext : DbContext
{
    public DbSet<Blog> Blogs { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseH2("Data Source=mydatabase;Storage=Memory", o => o.UseNodaTime());
}

public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Instant CreationTime { get; set; }
}
```

NodaTime members translate into H2's date and time operations. The connection string is the
classic ADO.NET form used by the engine (`Data Source`, `Storage`), not a JDBC URL or a
PostgreSQL connection string.

See the [usage guide](https://github.com/dimzon/h2sharp-efcore/blob/h2-provider/docs/USAGE.md)
and [DIVERGENCES.md](https://github.com/dimzon/h2sharp-efcore/blob/h2-provider/docs/DIVERGENCES.md)
for what the engine can and cannot run today.
