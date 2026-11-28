using Microsoft.EntityFrameworkCore;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Storage;

public class H2DatabaseCreatorDeleteTest
{
    [Fact]
    public void Delete_removes_tables_that_are_not_part_of_the_model()
    {
        using var context = new DeleteContext();
        context.Database.EnsureCreated();
        Execute(context, "CREATE TABLE \"ForeignTable\" (\"Id\" integer)");

        context.Database.EnsureDeleted();

        Assert.Equal(0, Scalar(context, "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ForeignTable'"));
    }

    private static void Execute(DbContext context, string sql)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static int Scalar(DbContext context, string sql)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        return (int)(long)command.ExecuteScalar()!;
    }

    private class DeleteContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2("Data Source=DeleteCreatorTest;Storage=Memory");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Entity>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Id).ValueGeneratedNever();
            });
        }

        private class Entity
        {
            public int Id { get; set; }
        }
    }
}
