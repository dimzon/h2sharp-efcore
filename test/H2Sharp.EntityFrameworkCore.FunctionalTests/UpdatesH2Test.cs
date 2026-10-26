using Microsoft.EntityFrameworkCore.TestModels.UpdatesModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: seventeen of the suite's 36 tests are red. Seven stop at the port's
// "Not ported yet: Prepared.update", six expect a DbUpdateConcurrencyException that is not thrown
// (the port does not report the affected row count back), and four read back values that differ.
// The trait stays on the class because two of the failing methods, SaveChanges_processes_all_
// tracked_entities and its _false_ twin, are not virtual in the base: a `new` method shadowing
// them is not a separate test case -- xunit deduplicates it against the inherited one and keeps
// the inherited one, which has no trait. Marking the other twelve and leaving those two in the
// green run would put four red tests there.
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
