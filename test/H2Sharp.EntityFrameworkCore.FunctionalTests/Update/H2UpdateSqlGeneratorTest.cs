using System.Text;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using H2Sharp.EntityFrameworkCore.Update.Internal;

namespace H2Sharp.EntityFrameworkCore.Update;

// H2 has no OUTPUT/RETURNING: inserts and updates that need generated columns are wrapped in
// FINAL TABLE, and a plain UPDATE/DELETE becomes a COUNT(*) over FINAL/OLD TABLE so EF can see
// the affected-row count. These baselines are the SQL the provider actually writes.
public class H2UpdateSqlGeneratorTest : UpdateSqlGeneratorTestBase
{
    protected override IUpdateSqlGenerator CreateSqlGenerator()
    {
        var helper = new H2SqlGenerationHelper(new RelationalSqlGenerationHelperDependencies());
        var typeMapping = new H2TypeMappingSource(
            TestServiceFactory.Instance.Create<TypeMappingSourceDependencies>(),
            TestServiceFactory.Instance.Create<RelationalTypeMappingSourceDependencies>(),
            helper,
            new H2SingletonOptions());

        return new H2UpdateSqlGenerator(new UpdateSqlGeneratorDependencies(helper, typeMapping));
    }

    protected override TestHelpers TestHelpers
        => H2TestHelpers.Instance;

    protected override string RowsAffected
        => "";

    public override void AppendInsertOperation_appends_insert_and_select_rowcount_if_no_store_generated_columns_exist_or_conditions_exist()
    {
        var sb = new StringBuilder();
        CreateSqlGenerator().AppendInsertOperation(sb, CreateInsertCommand(false, false), 0);

        AssertBaseline(
            """
INSERT INTO "dbo"."Ducks" ("Id", "Name", "Quacks", "ConcurrencyToken")
VALUES ($p0, $p1, $p2, $p3);
""",
            sb);
    }

    protected override void AppendDeleteOperation_creates_full_delete_command_text_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT COUNT(*) FROM OLD TABLE (DELETE FROM "dbo"."Ducks"
WHERE "Id" = $p0);
""",
            sb);

    protected override void AppendDeleteOperation_creates_full_delete_command_text_with_concurrency_check_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT COUNT(*) FROM OLD TABLE (DELETE FROM "dbo"."Ducks"
WHERE "Id" = $p0 AND "ConcurrencyToken" IS NULL);
""",
            sb);

    protected override void AppendInsertOperation_insert_if_store_generated_columns_exist_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Id", "Computed" FROM FINAL TABLE (INSERT INTO "dbo"."Ducks" ("Name", "Quacks", "ConcurrencyToken")
VALUES ($p0, $p1, $p2));
""",
            sb);

    protected override void AppendInsertOperation_for_store_generated_columns_but_no_identity_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Computed" FROM FINAL TABLE (INSERT INTO "dbo"."Ducks" ("Id", "Name", "Quacks", "ConcurrencyToken")
VALUES ($p0, $p1, $p2, $p3));
""",
            sb);

    protected override void AppendInsertOperation_for_only_identity_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Id" FROM FINAL TABLE (INSERT INTO "dbo"."Ducks" ("Name", "Quacks", "ConcurrencyToken")
VALUES ($p0, $p1, $p2));
""",
            sb);

    protected override void AppendInsertOperation_for_all_store_generated_columns_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Id", "Computed" FROM FINAL TABLE (INSERT INTO "dbo"."Ducks"
DEFAULT VALUES);
""",
            sb);

    protected override void AppendInsertOperation_for_only_single_identity_columns_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Id" FROM FINAL TABLE (INSERT INTO "dbo"."Ducks"
DEFAULT VALUES);
""",
            sb);

    protected override void AppendUpdateOperation_if_store_generated_columns_exist_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Computed" FROM FINAL TABLE (UPDATE "dbo"."Ducks" SET "Name" = $p0, "Quacks" = $p1, "ConcurrencyToken" = $p2
WHERE "Id" = $p3 AND "ConcurrencyToken" IS NULL);
""",
            sb);

    protected override void AppendUpdateOperation_if_store_generated_columns_dont_exist_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT COUNT(*) FROM FINAL TABLE (UPDATE "dbo"."Ducks" SET "Name" = $p0, "Quacks" = $p1, "ConcurrencyToken" = $p2
WHERE "Id" = $p3);
""",
            sb);

    protected override void AppendUpdateOperation_appends_where_for_concurrency_token_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT COUNT(*) FROM FINAL TABLE (UPDATE "dbo"."Ducks" SET "Name" = $p0, "Quacks" = $p1, "ConcurrencyToken" = $p2
WHERE "Id" = $p3 AND "ConcurrencyToken" IS NULL);
""",
            sb);

    protected override void AppendUpdateOperation_for_computed_property_verification(StringBuilder sb)
        => AssertBaseline(
            """
SELECT "Computed" FROM FINAL TABLE (UPDATE "dbo"."Ducks" SET "Name" = $p0, "Quacks" = $p1, "ConcurrencyToken" = $p2
WHERE "Id" = $p3);
""",
            sb);

    private static void AssertBaseline(string expected, StringBuilder actual)
        => Assert.Equal(
            (expected + Environment.NewLine).ReplaceLineEndings(),
            actual.ToString().ReplaceLineEndings());
}
