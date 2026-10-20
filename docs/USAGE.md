# Using H2 with Entity Framework Core

The **Entity Framework Core** provider for **H2Sharp** — the C# port of H2 Database 2.5. This
guide is about using it: connecting, mapping, migrating, the functions of H2, and what does not
work yet.

- [Quick start](#quick-start)
- [Getting the provider](#getting-the-provider)
- [Opening a connection](#opening-a-connection)
- [The context](#the-context)
- [Mapping](#mapping)
- [Migrations](#migrations)
- [Scaffolding](#scaffolding)
- [H2's own functions](#h2s-own-functions)
- [Window functions](#window-functions)
- [The catalogue](#the-catalogue)
- [The SQL it generates](#the-sql-it-generates)
- [What does not work yet](#what-does-not-work-yet)
- [Building and testing](#building-and-testing)

## Quick start

```csharp
using Microsoft.EntityFrameworkCore;

await using var ctx = new BlogContext();
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

public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
```

Two things in it wait for the engine, and the guide says where. The key of a new row is drawn
by the database, and H2 draws it with an identity column — the port does not read that yet, so
the quick start as written stands red.

The way round is to give the key to the application, and the guide shows how.

## Getting the provider

The provider is built for **.NET 8** and **.NET 10**, and so is the engine under it.

```
dotnet add package H2Sharp.EntityFrameworkCore
```

The provider sits on the engine, and the engine is a sibling checkout while it is not published
as a package. The path to it is overridden with the `H2SharpProject` property:

```xml
<ProjectReference Include="..\..\h2sharp\src\H2Sharp\H2Sharp.csproj" />
```

## Opening a connection

`UseH2` takes the connection string:

```csharp
optionsBuilder.UseH2("Data Source=mem:mydatabase");
```

The engine's ADO layer takes a JDBC URL, and the .NET world spells the same thing
`Data Source=mem:mydatabase`, so both are accepted:

| What is written | What the engine opens |
|---|---|
| `jdbc:h2:mem:mydatabase` | `jdbc:h2:mem:mydatabase` |
| `Data Source=mem:mydatabase` | `jdbc:h2:mem:mydatabase` |
| `Data Source=mem:mydatabase;MODE=PostgreSQL` | `jdbc:h2:mem:mydatabase;MODE=PostgreSQL` |

The settings a URL carries are the connection string's other keys. The translation is the
provider's, and the engine's own builder does the same thing — see
[docs/DIALECT.md](DIALECT.md).

A database in memory is gone when the last connection to it is closed, and the engine has no
file store yet, so `EnsureCreated` and friends work in memory and nowhere else for now. A
context that opens and closes its connection between commands loses the database with it, so
hold a connection open while the context works:

```csharp
using var connection = new H2Connection("jdbc:h2:mem:mydatabase");
connection.Open();

await using var ctx = new BlogContext(connection);
```

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseH2(connection);
```

## The context

Everything Entity Framework Core offers works as it does with any other provider: the
`DbContext`, the `DbSet`, the async methods, the transactions.

```csharp
await using var tx = await ctx.Database.BeginTransactionAsync();
try
{
    ctx.Blogs.Add(new() { Name = "BarBlog" });
    await ctx.SaveChangesAsync();
    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

## Mapping

The mapping is Entity Framework Core's own; what is H2's is the SQL that comes out of it and the
types it uses.

```csharp
public class Blog
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime Created { get; set; }
}
```

The types are H2's:

| C# | H2 |
|---|---|
| `bool` | `boolean` |
| `short` | `smallint` |
| `int` | `integer` |
| `long` | `bigint` |
| `float` | `real` |
| `double` | `double` |
| `decimal` | `numeric` |
| `string` | `varchar` |
| `char` | `char` |
| `string` (long) | `clob` — H2 keeps such values out of memory and streams them; asked for by name |
| `byte[]` | `varbinary` |
| `Guid` | `uuid` |
| `DateTime` | `timestamp` |
| `DateTimeOffset` | `timestamp with time zone` |
| `DateOnly` | `date` |
| `TimeOnly` | `time` |

An identifier is quoted the way H2 documents it, and an unquoted one folds to upper case, so
the default name translator folds to upper case too — that is both H2's rule and the spelling a
hand-written H2 client would use.

### Value generation

H2 2.5 has no `INSERT … RETURNING`; it has `NEXT VALUE FOR`. The key of a new row is therefore
drawn from a sequence, and the value of the sequence is read back with `CURRENT VALUE FOR`:

```csharp
modelBuilder.Entity<Blog>()
    .Property(b => b.Id)
    .ValueGeneratedOnAdd()
    .HasSequence("blog_ids");
```

`IDENTITY` is the other way — `GENERATED BY DEFAULT AS IDENTITY` — and the reference supports
it, but the port does not read it yet. That is what Entity Framework Core asks for by its
convention, so until the port grows, the key is given by the application:

```csharp
modelBuilder.Entity<Article>()
    .Property(a => a.Id)
    .ValueGeneratedNever();
```

### Strings

A string is a `CHARACTER VARYING` in H2 2.5 — the documentation says it plainly: *for
variable-length strings use CHARACTER VARYING*. Without a length it takes the maximum, so a
string of any size fits. `CHARACTER LARGE OBJECT` is a different thing: H2 keeps such values
out of memory and streams them, and it is meant for documents and texts of arbitrary size. A
column that is one is asked for by name:

```csharp
modelBuilder.Entity<Article>()
    .Property(a => a.Title)
    .HasColumnType("clob");
```

The port has the storage of a large object (`ValueLob`, `LobData`, MVStore) but not the way out
of it into a result, so such a column cannot be read back yet.

## Migrations

The migrations of Entity Framework Core work, and the SQL they generate is H2's:

```bash
dotnet ef migrations add Initial
dotnet ef database update
```

The migration SQL generator is H2's, not PostgreSQL's: the types are H2's, the identifiers are
quoted, and the paging is `LIMIT`/`OFFSET`.

## Scaffolding

Scaffolding reads the catalogue, and the catalogue is what the engine has not grown yet. The
provider's part is written — `H2DatabaseModelFactory` reads `INFORMATION_SCHEMA` the way H2 2.5
keeps it — and it refuses with a message naming the missing engine feature rather than
returning an empty model: an empty model would be indistinguishable from an empty database, and
every migration made from it would try to recreate tables that are there.

```bash
dotnet ef dbcontext scaffold "Data Source=mem:mydatabase" H2Sharp
```

The command is ready and waits for the engine.

## H2's own functions

`EF.Functions` reaches the built-in functions of the database. The provider offers the ones H2
has, each checked against the engine:

```csharp
EF.Functions.DateAdd("DAY", 1, blog.Created)   // DATEADD('DAY', 1, ...)
EF.Functions.Locate(blog.Name, "foo")           // LOCATE(...) -- H2 takes the substring first
EF.Functions.Extract("YEAR", blog.Created)      // YEAR(...)
EF.Functions.LeftPad(blog.Name, 10, "0")        // LPAD(...)
```

| Group | Functions |
|---|---|
| Date and time | `DateAdd`, `DateDiff`, `DateTrunc`, `Extract`, `DayName`, `MonthName` |
| Strings | `ILike`, `Locate`, `LeftPad`, `RightPad`, `Translate`, `Repeat`, `Space`, `QuoteIdentifier`, `Soundex`, `SoundexDifference`, `Ascii`, `Char`, `Chr`, `StringDecode`, `StringEncode`, `StringToUtf8`, `Utf8ToString`, `HexToRaw`, `RawToHex`, `Insert`, `ConcatWithSeparator` |
| Numbers | `Sign`, `Truncate`, `Cotangent`, `RoundMagic` |
| Bits | `BitAnd`, `BitOr`, `BitXor`, `BitNot`, `BitNand`, `BitNor`, `BitXnor`, `BitGet`, `BitCount`, `LeftShift`, `RightShift`, `UnsignedLeftShift`, `UnsignedRightShift`, `RotateLeft`, `RotateRight` |
| Compressed text | `Compress`, `Expand` |

Two of them are worth a word. `ILike` is an operator in H2, not a function, so the text it
stands on has to be a column. And `LOCATE` takes the substring first, the other way round from
the function of the SQL standard.

The list of what is offered and what the engine does not run yet is in
[docs/DIVERGENCES.md](DIVERGENCES.md).

## Window functions

H2 2.5 has the window functions — `ROW_NUMBER`, `RANK`, `DENSE_RANK`, `LAG`, `LEAD`,
`FIRST_VALUE`, `LAST_VALUE`, `NTH_VALUE`, `NTILE`, `PERCENT_RANK`, `CUME_DIST` — but Entity
Framework Core 9 does not translate them into a query, so there is nothing for the provider to
write. A provider cannot add what the framework never asks for.

Where the framework does ask, the port is the one that has to catch up: the port does not read
`OVER` yet.

## The catalogue

The catalogue is `INFORMATION_SCHEMA`, and the provider reads it the way H2 2.5 keeps it: the
objects in `INFORMATION_SCHEMA.TABLES`, the columns in `INFORMATION_SCHEMA.COLUMNS`, the keys
in `INFORMATION_SCHEMA.TABLE_CONSTRAINTS` and `INFORMATION_SCHEMA.KEY_COLUMN_USAGE`, the
references in `INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS`.

The port has no `INFORMATION_SCHEMA` yet, so everything that reads the catalogue — the
scaffolding, and the migrations that compare the model with the database — waits for the
engine. What the provider does meanwhile is written and checked against the reference; see
[docs/DIALECT.md](DIALECT.md).

## The SQL it generates

| | |
|---|---|
| Identifiers | always quoted, the way H2 documents it: an unquoted identifier folds to upper case and a quoted one does not |
| Types | H2's own: `varchar`, `char`, `numeric`, `varbinary`, `uuid`, `json`, `clob`, `blob`, `timestamp`, `time`, `boolean`, `smallint`, `integer`, `bigint`, `real`, `double` |
| Paging | `LIMIT n` / `OFFSET n` |
| Concatenation | `\|\|`, which H2 has |
| Parameters | named `@name`, which the engine rewrites into `?` before the statement is parsed |
| Table alias | without the `AS`: `FROM "Blogs" "b"` — the grammar allows both spellings ("[ [ AS ] newTableAlias ]"), and this is the one the engine reads |

The capabilities the provider declares are H2 2.5's, not the engine's current ones: a provider
that declared the engine's abilities would be describing a database that does not exist.

## What does not work yet

The engine is a port in work, and the provider is written for H2 2.5 rather than for the port as
it stands. The things that wait for the engine:

| What | What happens |
|---|---|
| `INFORMATION_SCHEMA` | `Table "INFORMATION_SCHEMA" not found` — the catalogue, and with it the scaffolding |
| identity columns | `CREATE TABLE … GENERATED BY DEFAULT AS IDENTITY …` — the parser does not reach identity |
| `CREATE SEQUENCE` | `CREATE SEQUENCE … START WITH n` — the parser |
| `NEXT VALUE FOR` / `CURRENT VALUE FOR` | the parser |
| `JOIN` | `FROM T3 a LEFT JOIN T3 b` — expected `statement` |
| `AS` in front of a table alias | `SELECT a.Id FROM T3 AS a` — expected `statement` |
| `IN (subquery)` | `IN with a subquery is not ported` |
| `MERGE` | `MERGE INTO T AS t USING U AS u` — expected `statement` |
| `OVER` | window functions |
| `COMMENT ON` | `COMMENT ON TABLE …` |
| large objects | `ValueLob.copyToResult: lob storage is not ported` — the storage of a large object is in the port (`ValueLob`, `LobData`, MVStore); the copy from the storage into the result is not, so a column asked for as `clob` cannot be read back |
| file databases | `Not ported yet: Engine.openSession: persistent databases need the file store` |

The full list, with what each of them costs, is in
[docs/DIVERGENCES.md](DIVERGENCES.md).

## Building and testing

The engine is a sibling checkout; the path is overridden with the `H2SharpProject` property.

```bash
dotnet build H2Sharp.EntityFrameworkCore.sln -c Release
dotnet test H2Sharp.EntityFrameworkCore.sln -c Release --no-build
```

The tests come in two runs. The first is what the provider can do; the second is what the
engine cannot do yet, and every test in it is a defect of the engine:

```bash
# green: everything that can pass does pass
dotnet test H2Sharp.EntityFrameworkCore.sln -c Release --no-build --filter "Category!=BlockedByPort"

# the port's defects, listed
dotnet test H2Sharp.EntityFrameworkCore.sln -c Release --no-build --filter "Category=BlockedByPort"
```
