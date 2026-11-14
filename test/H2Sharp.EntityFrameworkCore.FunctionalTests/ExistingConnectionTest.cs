using System.Data;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

public class ExistingConnectionTest
{
    // See aspnet/Data#135
    [Fact]
    public async Task Can_use_an_existing_closed_connection()
        => await Can_use_an_existing_closed_connection_test(openConnection: false);

    [Fact]
    public async Task Can_use_an_existing_open_connection()
        => await Can_use_an_existing_closed_connection_test(openConnection: true);

    private static async Task Can_use_an_existing_closed_connection_test(bool openConnection)
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkH2()
            .BuildServiceProvider();

        // The database of the store is the one of the connection string, and the database lives
        // as long as the data source that owns it: the two variants of the test fill their own
        // database with the same rows, so they must not share one.
        await using (var store = H2TestStore.Create(openConnection ? "ExistingConnectionOpen" : "ExistingConnectionClosed"))
        {
            // The in-memory database lives as long as the store's own connection, which is
            // opened here and stays open; the connection under test is a separate one.
            store.OpenConnection();

            await using (var seed = new CustomerContext(new DbContextOptionsBuilder<CustomerContext>().UseH2(store.ConnectionString).Options))
            {
                await seed.Database.EnsureCreatedAsync();
                seed.Customers.AddRange(
                    new Customer { CustomerId = "ALFKI", CompanyName = "Alfreds" },
                    new Customer { CustomerId = "ANATR", CompanyName = "Ana Trujillo" },
                    new Customer { CustomerId = "ANTON", CompanyName = "Antonio Moreno" });
                await seed.SaveChangesAsync();
            }

            var openCount = 0;
            var closeCount = 0;

            await using (var connection = (H2Connection)store.DataSource.CreateConnection())
            {
                if (openConnection)
                {
                    await connection.OpenAsync();
                }

                connection.StateChange += (_, a) =>
                {
                    if (a.CurrentState == ConnectionState.Open)
                    {
                        openCount++;
                    }
                    else if (a.CurrentState == ConnectionState.Closed)
                    {
                        closeCount++;
                    }
                };

                await using (var context = new CustomerContext(serviceProvider, connection))
                {
                    Assert.Equal(3, await context.Customers.CountAsync());
                }

                if (openConnection)
                {
                    Assert.Equal(ConnectionState.Open, connection.State);
                    Assert.Equal(0, openCount);
                    Assert.Equal(0, closeCount);
                }
                else
                {
                    Assert.Equal(ConnectionState.Closed, connection.State);
                    Assert.Equal(1, openCount);
                    Assert.Equal(1, closeCount);
                }
            }
        }
    }

    private class CustomerContext(DbContextOptions<CustomerContext> options) : DbContext(options)
    {
        public CustomerContext(IServiceProvider serviceProvider, H2Connection connection)
            : this(new DbContextOptionsBuilder<CustomerContext>()
                .UseH2(connection)
                .UseInternalServiceProvider(serviceProvider)
                .Options)
        {
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Customer>(
                b =>
                {
                    b.HasKey(c => c.CustomerId);
                    ((EntityTypeBuilder)b).ToTable("Customers");
                });
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class Customer
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string CustomerId { get; set; }

        // ReSharper disable once UnusedMember.Local
        public string CompanyName { get; set; }

        // ReSharper disable once UnusedMember.Local
        public string Fax { get; set; }
    }
}
