using System.Collections.Generic;
using System.Data.Common;

using Microsoft.EntityFrameworkCore;

using H2Sharp.EntityFrameworkCore.TestUtilities;

#nullable enable

namespace H2Sharp.EntityFrameworkCore;

/// <summary>
/// A query of the rows of a table: the statement EF Core makes for it, and the rows that
/// come back.
/// </summary>
public class H2QueryTests
{
    private readonly IServiceProvider _serviceProvider = new ServiceCollection()
        .AddEntityFrameworkH2()
        .BuildServiceProvider();

    private sealed class Item
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";
    }

    private class ItemContext(IServiceProvider serviceProvider, DbConnection connection) : DbContext
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;

        public DbSet<Item> Items { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseH2(connection)
                .UseInternalServiceProvider(_serviceProvider);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Item>().HasKey(e => e.Id);
    }

    [Fact]
    public void The_rows_of_a_table_come_back()
    {
        using var connection = new H2Connection("jdbc:h2:mem:query_rows");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Add(new Item { Name = "one" });
        context.Add(new Item { Name = "two" });
        context.SaveChanges();

        List<string> names = context.Items.OrderBy(i => i.Id).Select(i => i.Name).ToList();

        Assert.Equal(new[] { "one", "two" }, names);
    }

    [Fact]
    public void A_row_is_changed_and_removed()
    {
        using var connection = new H2Connection("jdbc:h2:mem:query_change");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        var item = context.Add(new Item { Name = "one" }).Entity;
        context.SaveChanges();

        item.Name = "two";
        context.SaveChanges();
        Assert.Equal("two", context.Items.Single().Name);

        context.Remove(item);
        context.SaveChanges();
        Assert.Empty(context.Items);
    }

    [Fact]
    public void A_row_is_asked_for()
    {
        using var connection = new H2Connection("jdbc:h2:mem:query_where");
        connection.Open();

        using var context = new ItemContext(_serviceProvider, connection);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

        context.Add(new Item { Name = "one" });
        context.Add(new Item { Name = "two" });
        context.SaveChanges();

        Assert.Equal("two", context.Items.Single(i => i.Name == "two").Name);
        Assert.Equal(2, context.Items.Count(i => i.Name != "three"));
    }
}
