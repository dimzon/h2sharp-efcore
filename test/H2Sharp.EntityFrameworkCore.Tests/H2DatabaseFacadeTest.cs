namespace H2Sharp.EntityFrameworkCore;

public class H2DatabaseFacadeTest
{
    [Fact]
    public void IsH2_when_using_OnConfiguring()
    {
        using var context = new H2OnConfiguringContext();

        Assert.True(context.Database.IsH2());
    }

    [Fact]
    public void IsH2_in_OnModelCreating_when_using_OnConfiguring()
    {
        using var context = new H2OnModelContext();
        var _ = context.Model; // Trigger context initialization

        Assert.True(context.IsH2Set);
    }

    [Fact]
    public void IsH2_in_constructor_when_using_OnConfiguring()
    {
        using var context = new H2ConstructorContext();
        var _ = context.Model; // Trigger context initialization

        Assert.True(context.IsH2Set);
    }

    [Fact]
    public void Cannot_use_IsH2_in_OnConfiguring()
    {
        using var context = new H2UseInOnConfiguringContext();

        Assert.Equal(
            CoreStrings.RecursiveOnConfiguring,
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var _ = context.Model; // Trigger context initialization
                }).Message);
    }

    [Fact]
    public void IsH2_when_using_constructor()
    {
        using var context = new ProviderContext(
            new DbContextOptionsBuilder().UseH2("Database=Maltesers").Options);

        Assert.True(context.Database.IsH2());
    }

    [Fact]
    public void IsH2_in_OnModelCreating_when_using_constructor()
    {
        using var context = new ProviderOnModelContext(
            new DbContextOptionsBuilder().UseH2("Database=Maltesers").Options);
        var _ = context.Model; // Trigger context initialization

        Assert.True(context.IsH2Set);
    }

    [Fact]
    public void IsH2_in_constructor_when_using_constructor()
    {
        using var context = new ProviderConstructorContext(
            new DbContextOptionsBuilder().UseH2("Database=Maltesers").Options);
        var _ = context.Model; // Trigger context initialization

        Assert.True(context.IsH2Set);
    }

    [Fact]
    public void Cannot_use_IsH2_in_OnConfiguring_with_constructor()
    {
        using var context = new ProviderUseInOnConfiguringContext(
            new DbContextOptionsBuilder().UseH2("Database=Maltesers").Options);

        Assert.Equal(
            CoreStrings.RecursiveOnConfiguring,
            Assert.Throws<InvalidOperationException>(
                () =>
                {
                    var _ = context.Model; // Trigger context initialization
                }).Message);
    }

    /*
    [Fact]
    public void Not_IsH2_when_using_different_provider()
    {
        using var context = new ProviderContext(
            new DbContextOptionsBuilder().UseInMemoryDatabase("Maltesers").Options);

        Assert.False(context.Database.IsH2());
    }*/

    private class ProviderContext : DbContext
    {
        protected ProviderContext()
        {
        }

        public ProviderContext(DbContextOptions options)
            : base(options)
        {
        }

        public bool? IsH2Set { get; protected set; }
    }

    private class H2OnConfiguringContext : ProviderContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseH2("Database=Maltesers");
    }

    private class H2OnModelContext : H2OnConfiguringContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => IsH2Set = Database.IsH2();
    }

    private class H2ConstructorContext : H2OnConfiguringContext
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public H2ConstructorContext()
        {
            IsH2Set = Database.IsH2();
        }
    }

    private class H2UseInOnConfiguringContext : H2OnConfiguringContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            IsH2Set = Database.IsH2();
        }
    }

    private class ProviderOnModelContext(DbContextOptions options) : ProviderContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => IsH2Set = Database.IsH2();
    }

    private class ProviderConstructorContext : ProviderContext
    {
        // ReSharper disable once VirtualMemberCallInConstructor
        public ProviderConstructorContext(DbContextOptions options)
            : base(options)
        {
            IsH2Set = Database.IsH2();
        }
    }

    private class ProviderUseInOnConfiguringContext(DbContextOptions options) : ProviderContext(options)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => IsH2Set = Database.IsH2();
    }
}
