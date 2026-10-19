using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

// ReSharper disable StringLiteralTypo
namespace H2Sharp.EntityFrameworkCore;

public class ConnectionSpecificationTest
{
    [Fact]
    public async Task Can_specify_connection_string_in_OnConfiguring()
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<StringInOnConfiguringContext>()
            .BuildServiceProvider();

        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = serviceProvider.GetRequiredService<StringInOnConfiguringContext>();

        Assert.True(await context.Customers.AnyAsync());
    }

    [Fact]
    public async Task Can_specify_connection_string_in_OnConfiguring_with_default_service_provider()
    {
        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = new StringInOnConfiguringContext();

        Assert.True(await context.Customers.AnyAsync());
    }

    private class StringInOnConfiguringContext : NorthwindContextBase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2(H2TestStore.NorthwindConnectionString, b => b.ApplyConfiguration());
    }

    [Fact]
    public async Task Can_specify_connection_in_OnConfiguring()
    {
        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => new H2Connection(H2TestStore.NorthwindConnectionString))
            .AddDbContext<ConnectionInOnConfiguringContext>().BuildServiceProvider();

        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = serviceProvider.GetRequiredService<ConnectionInOnConfiguringContext>();

        Assert.True(await context.Customers.AnyAsync());
    }

    [Fact]
    public async Task Can_specify_connection_in_OnConfiguring_with_default_service_provider()
    {
        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = new ConnectionInOnConfiguringContext(new H2Connection(H2TestStore.NorthwindConnectionString));

        Assert.True(await context.Customers.AnyAsync());
    }

    private class ConnectionInOnConfiguringContext(H2Connection connection) : NorthwindContextBase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2(connection, b => b.ApplyConfiguration());

        public override void Dispose()
        {
            connection.Dispose();
            base.Dispose();
        }
    }

    // ReSharper disable once UnusedMember.Local
    private class StringInConfigContext : NorthwindContextBase
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2("Database=Crunchie", b => b.ApplyConfiguration());
    }

    [Fact]
    public void Throws_if_no_connection_found_in_config_without_UseH2()
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<NoUseH2Context>().BuildServiceProvider();

        using var context = serviceProvider.GetRequiredService<NoUseH2Context>();

        Assert.Equal(
            CoreStrings.NoProviderConfigured,
            Assert.Throws<InvalidOperationException>(() => context.Customers.Any()).Message);
    }

    [Fact]
    public void Throws_if_no_config_without_UseH2()
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<NoUseH2Context>().BuildServiceProvider();
        using var context = serviceProvider.GetRequiredService<NoUseH2Context>();

        Assert.Equal(
            CoreStrings.NoProviderConfigured,
            Assert.Throws<InvalidOperationException>(() => context.Customers.Any()).Message);
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    private class NoUseH2Context : NorthwindContextBase;

    [Fact]
    public async Task Can_depend_on_DbContextOptions()
    {
        var serviceProvider = new ServiceCollection()
            .AddScoped(_ => new H2Connection(H2TestStore.NorthwindConnectionString))
            .AddDbContext<OptionsContext>()
            .BuildServiceProvider();

        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = serviceProvider.GetRequiredService<OptionsContext>();

        Assert.True(await context.Customers.AnyAsync());
    }

    [Fact]
    public async Task Can_depend_on_DbContextOptions_with_default_service_provider()
    {
        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = new OptionsContext(
            new DbContextOptions<OptionsContext>(),
            new H2Connection(H2TestStore.NorthwindConnectionString));

        Assert.True(await context.Customers.AnyAsync());
    }

    private class OptionsContext(DbContextOptions<OptionsContext> options, H2Connection connection)
        : NorthwindContextBase(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Assert.Same(options, optionsBuilder.Options);

            optionsBuilder.UseH2(connection, b => b.ApplyConfiguration());

            Assert.NotSame(options, optionsBuilder.Options);
        }

        public override void Dispose()
        {
            connection.Dispose();
            base.Dispose();
        }
    }

    [Fact]
    public async Task Can_depend_on_non_generic_options_when_only_one_context()
    {
        var serviceProvider = new ServiceCollection()
            .AddDbContext<NonGenericOptionsContext>()
            .BuildServiceProvider();

        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = serviceProvider.GetRequiredService<NonGenericOptionsContext>();

        Assert.True(await context.Customers.AnyAsync());
    }

    [Fact]
    public async Task Can_depend_on_non_generic_options_when_only_one_context_with_default_service_provider()
    {
        await using var _ = await H2TestStore.GetNorthwindStoreAsync();
        await using var context = new NonGenericOptionsContext(new DbContextOptions<DbContext>());

        Assert.True(await context.Customers.AnyAsync());
    }

    private class NonGenericOptionsContext(DbContextOptions options) : NorthwindContextBase(options)
    {
        private readonly DbContextOptions _options = options;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Assert.Same(_options, optionsBuilder.Options);

            optionsBuilder.UseH2(H2TestStore.NorthwindConnectionString, b => b.ApplyConfiguration());

            Assert.NotSame(_options, optionsBuilder.Options);
        }
    }

    private class NorthwindContextBase : DbContext
    {
        protected NorthwindContextBase()
        {
        }

        protected NorthwindContextBase(DbContextOptions options)
            : base(options)
        {
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public DbSet<Customer> Customers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Customer>(
                b =>
                {
                    b.HasKey(c => c.CustomerId);
                    b.ToTable("Customers");
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

    #region Added for H2

    [Fact]
    public async Task Can_create_admin_connection_with_data_source()
    {
        await using var dataSource = H2DataSource.Create(H2TestStore.NorthwindConnectionString);

        await using var _ = await H2TestStore.GetNorthwindStoreAsync();

        var optionsBuilder = new DbContextOptionsBuilder<GeneralOptionsContext>();
        optionsBuilder.UseH2(dataSource, b => b.ApplyConfiguration());
        await using var context = new GeneralOptionsContext(optionsBuilder.Options);

        var relationalConnection = context.GetService<IH2RelationalConnection>();
        await using var adminConnection = relationalConnection.CreateAdminConnection();

        Assert.Equal("postgres", new H2ConnectionStringBuilder(adminConnection.ConnectionString).Database);

        await adminConnection.OpenAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Can_create_admin_connection_with_connection_string()
    {
        await using var _ = await H2TestStore.GetNorthwindStoreAsync();

        var optionsBuilder = new DbContextOptionsBuilder<GeneralOptionsContext>();
        optionsBuilder.UseH2(H2TestStore.NorthwindConnectionString, b => b.ApplyConfiguration());
        await using var context = new GeneralOptionsContext(optionsBuilder.Options);

        var relationalConnection = context.GetService<IH2RelationalConnection>();
        await using var adminConnection = relationalConnection.CreateAdminConnection();

        Assert.Equal("postgres", new H2ConnectionStringBuilder(adminConnection.ConnectionString).Database);

        await adminConnection.OpenAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Can_create_admin_connection_with_connection()
    {
        await using var connection = new H2Connection(H2TestStore.NorthwindConnectionString);
        connection.Open();

        await using var _ = await H2TestStore.GetNorthwindStoreAsync();

        var optionsBuilder = new DbContextOptionsBuilder<GeneralOptionsContext>();
        optionsBuilder.UseH2(connection, b => b.ApplyConfiguration());
        await using var context = new GeneralOptionsContext(optionsBuilder.Options);

        var relationalConnection = context.GetService<IH2RelationalConnection>();
        await using var adminConnection = relationalConnection.CreateAdminConnection();

        Assert.Equal("postgres", new H2ConnectionStringBuilder(adminConnection.ConnectionString).Database);

        adminConnection.Open();
    }

    private class GeneralOptionsContext(DbContextOptions<GeneralOptionsContext> options) : NorthwindContextBase(options);

    #endregion
}
