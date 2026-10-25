using Microsoft.EntityFrameworkCore.TestModels.UpdatesModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the fixture's model carries UNIQUE and FOREIGN KEY table constraints, and the
// port's CREATE TABLE reads only the primary key of them ("expected PRIMARY"). The reference
// accepts the statement the provider writes (checked on the jar).
[Trait("Category", "BlockedByPort")]
public class UpdatesH2Test : UpdatesRelationalTestBase<UpdatesH2Test.UpdatesH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public UpdatesH2Test(UpdatesH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
    }

    public override void Identifiers_are_generated_correctly()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(
                LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameThatIsUsedToVerifyThatTheStoreIdentifierGenerationLengthLimitIsWorkingCorrectly
            ));
        Assert.Equal(
            "LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameThatI~",
            entityType.GetTableName());
        Assert.Equal(
            "PK_LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameTh~",
            entityType.GetKeys().Single().GetName());
        Assert.Equal(
            "FK_LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameTh~",
            entityType.GetForeignKeys().Single().GetConstraintName());
        Assert.Equal(
            "IX_LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameTh~",
            entityType.GetIndexes().Single().GetDatabaseName());

        var entityType2 = context.Model.FindEntityType(
            typeof(
                LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameThatIsUsedToVerifyThatTheStoreIdentifierGenerationLengthLimitIsWorkingCorrectlyDetails
            ));

        Assert.Equal(
            "LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameThat~1",
            entityType2.GetTableName());
        Assert.Equal(
            "PK_LoginDetails",
            entityType2.GetKeys().Single().GetName());
        Assert.Equal(
            "ExtraPropertyWithAnExtremelyLongAndOverlyConvolutedNameThatIsU~",
            entityType2.GetProperties().ElementAt(1).GetColumnName(StoreObjectIdentifier.Table(entityType2.GetTableName())));
        Assert.Equal(
            "ExtraPropertyWithAnExtremelyLongAndOverlyConvolutedNameThatIs~1",
            entityType2.GetProperties().ElementAt(2).GetColumnName(StoreObjectIdentifier.Table(entityType2.GetTableName())));
        Assert.Equal(
            "IX_LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameT~1",
            entityType2.GetIndexes().Single().GetDatabaseName());
    }

    public class UpdatesH2Fixture : UpdatesRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            // H2 has no extensions: the UUID generator the uuid-ossp extension provided upstream
            // is the built-in RANDOM_UUID().
            modelBuilder.Entity<ProductBase>()
                .Property(p => p.Id).HasDefaultValueSql("RANDOM_UUID()");

            // Upstream declared this unique index with a "Name" IS NOT NULL partial filter. H2 2.5
            // has no partial index, but it does not need one here: its unique index counts NULLs as
            // distinct by default, so rows with a NULL name never collide with anything.
            modelBuilder.Entity<Product>().HasIndex(p => new { p.Name, p.Price }).IsUnique();

            modelBuilder.Entity<Rodney>().Property(r => r.Concurrency).HasColumnType("timestamp without time zone");
        }
    }
}
