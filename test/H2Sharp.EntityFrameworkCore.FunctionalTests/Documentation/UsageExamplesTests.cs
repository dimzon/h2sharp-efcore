// Copyright 2026 H2Sharp contributors.
// Licensed under the MIT License (see LICENSE).

using System.Data.Common;

using Microsoft.EntityFrameworkCore;

namespace H2Sharp.EntityFrameworkCore.Documentation;

/// <summary>
///     The examples of docs/USAGE.md, as tests.
/// </summary>
/// <remarks>
///     <para>
///         The guide is only worth reading if it is true, so what it says is written here and
///         checked. A test that stands red because the engine cannot do the thing yet is marked
///         as such: it is the engine's defect, and the guide says so too.
///     </para>
/// </remarks>
public class UsageExamplesTests
{
    private class BlogContext : DbContext
    {
        private readonly DbConnection _connection;

        public BlogContext(DbConnection connection)
            => _connection = connection;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2(_connection);

        public DbSet<Blog> Blogs { get; set; } = null!;
    }

    private class Blog
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime Created { get; set; }
    }

    private class ArticleContext : DbContext
    {
        private readonly DbConnection _connection;

        public ArticleContext(DbConnection connection)
            => _connection = connection;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2(_connection);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            // By its convention Entity Framework Core has the database draw the key of a new row,
            // and H2 draws it with an identity column. The key is given here instead, so that the
            // table can be made without one.
            => modelBuilder.Entity<Article>().Property(a => a.Id).ValueGeneratedNever();

        public DbSet<Article> Articles { get; set; } = null!;
    }

    private class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
    }

    [Fact]
    [Trait("Category", "BlockedByPort")]
    public async Task The_quick_start_works()
    {
        using var connection = new H2Connection("jdbc:h2:mem:documentation");
        connection.Open();

        await using var ctx = new BlogContext(connection);

        await ctx.Database.EnsureDeletedAsync();

        // The key of a new row is drawn by the database, and the database draws it with an
        // identity column -- which the port does not read yet.
        await ctx.Database.EnsureCreatedAsync();

        ctx.Blogs.Add(new() { Name = "FooBlog", Created = new DateTime(2020, 3, 5, 10, 20, 30) });
        await ctx.SaveChangesAsync();

        var fBlogs = await ctx.Blogs.Where(b => b.Name.StartsWith("F")).ToListAsync();

        Assert.Equal("FooBlog", Assert.Single(fBlogs).Name);
    }

    [Fact]
    public async Task A_context_whose_key_the_application_gives_works()
    {
        using var connection = new H2Connection("jdbc:h2:mem:documentation-explicitkey");
        connection.Open();

        await using var ctx = new ArticleContext(connection);

        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        ctx.Articles.Add(new() { Id = 1, Title = "FooArticle" });
        await ctx.SaveChangesAsync();

        var articles = await ctx.Articles.Where(a => a.Title.StartsWith("F")).ToListAsync();

        Assert.Equal("FooArticle", Assert.Single(articles).Title);
    }

    [Fact]
    public async Task A_function_of_H2_is_called_in_the_database()
    {
        using var connection = new H2Connection("jdbc:h2:mem:documentation-functions");
        connection.Open();

        await using var ctx = new ArticleContext(connection);

        await ctx.Database.EnsureDeletedAsync();
        await ctx.Database.EnsureCreatedAsync();

        ctx.Articles.Add(new() { Id = 1, Title = "abc" });
        await ctx.SaveChangesAsync();

        var rows = await ctx.Articles
            .Select(a => new
            {
                // H2 2.5 pads on the left: LPAD('abc', 6, '*') is '***abc'.
                Padded = EF.Functions.LeftPad(a.Title, 6, "*"),
                // H2 2.5 takes the substring first in LOCATE.
                Located = EF.Functions.Locate(a.Title, "b"),
            })
            .ToListAsync();

        var row = Assert.Single(rows);

        Assert.Equal("***abc", row.Padded);
        Assert.Equal(2, row.Located);
    }
}
