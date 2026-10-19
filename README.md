# H2Sharp.EntityFrameworkCore

Entity Framework Core provider for **H2Sharp** — the C# port of H2 Database 2.5.

> **Status: in progress.** The provider generates H2's SQL, and that SQL is checked against
> the reference engine on the JVM rather than against the port. The database-level part of
> EF Core's own specification suite is waiting for the port to grow: identity DDL,
> `INFORMATION_SCHEMA`, `CREATE SEQUENCE`, `JOIN`, and the `AS` in front of a table alias.
   > What is missing and who is to fix it is listed in
   > [docs/DIVERGENCES.md](docs/DIVERGENCES.md).

   The provider is built for **.NET 8** and **.NET 10**; the engine under it has both as well.


## What it is

A fork of the **EFCore.PG** provider (Npgsql's provider for PostgreSQL), with PostgreSQL's
surface replaced by H2's: the type mappings, the migrations SQL generator, the connection and
the DDL are H2's. The PostgreSQL-only features went with it — ranges, multiranges, the
network address types, full-text search, PostGIS, and the rest are listed in
[docs/DIVERGENCES.md](docs/DIVERGENCES.md).

The point of the fork is to keep EF Core's own specification suite as the measure of the
provider: the tests are inherited from EF Core, not written for H2.

## Tests

The suite is split in two, because a test that fails because the engine does not read a
function yet is a **port defect**, not a provider defect — and it stays in the suite, marked,
so the gap remains visible:

```sh
# green: everything that can pass does pass
dotnet test H2Sharp.EntityFrameworkCore.sln -c Release --no-build --filter "Category!=BlockedByPort"

# the port's defects, listed
dotnet test H2Sharp.EntityFrameworkCore.sln -c Release --no-build --filter "Category=BlockedByPort"
```

What each defect is and who fixes it is in [docs/DIVERGENCES.md](docs/DIVERGENCES.md). The
rules the work follows are in [AGENTS.md](AGENTS.md).

## Using it

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
        => optionsBuilder.UseH2("Data Source=mem:mydatabase");
}
```

The connection string is the ADO.NET spelling. H2Sharp's ADO layer takes a JDBC URL, so
`Data Source=mem:mydatabase` is translated to `jdbc:h2:mem:mydatabase`; a URL is accepted as
it is. The settings a URL carries — `MODE=PostgreSQL` and the like — are the connection
string's other keys.

## H2's own functions

`EF.Functions` reaches the built-in functions of the database. The provider offers the ones
H2 has and the engine can run — the date and time ones, the string, binary, number and bit
ones — each checked against the engine:

```csharp
EF.Functions.DateAdd("DAY", 1, blog.Created)   // DATEADD('DAY', 1, ...)
EF.Functions.Locate(blog.Name, "foo")           // LOCATE(...) -- H2 takes the substring first
EF.Functions.Extract("YEAR", blog.Created)      // YEAR(...)
EF.Functions.LeftPad(blog.Name, 10, "0")        // LPAD(...)
```

The list of what is offered and what the engine does not run yet is in
[docs/DIVERGENCES.md](docs/DIVERGENCES.md).

## Related projects

* The engine this provider talks to: **H2Sharp**, <https://github.com/dimzon/h2sharp> — a
  sibling checkout, `../h2sharp`.
* The provider's own documentation: [docs/DIVERGENCES.md](docs/DIVERGENCES.md) for what
  differs from EF Core and from H2, [docs/DIALECT.md](docs/DIALECT.md) for the SQL it
  generates, and [docs/ROADMAP.md](docs/ROADMAP.md) for the plan.
* The provider's origin: [UPSTREAM.md](UPSTREAM.md) records the EFCore.PG commit this fork
  starts from.

## Licensing

The fork keeps the PostgreSQL License of EFCore.PG; see [LICENSE](LICENSE) and
[NOTICE](NOTICE). The engine it targets is dual-licensed under the MPL 2.0 and the EPL 1.0.
