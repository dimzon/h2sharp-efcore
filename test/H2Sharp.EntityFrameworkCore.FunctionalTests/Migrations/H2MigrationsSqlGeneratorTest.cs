using Microsoft.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Migrations.Operations;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Migrations;

public class H2MigrationsSqlGeneratorTest() : MigrationsSqlGeneratorTestBase(
    H2TestHelpers.Instance,
    new ServiceCollection().AddEntityFrameworkH2NetTopologySuite(),
    H2TestHelpers.Instance.AddProviderOptions(
        ((IRelationalDbContextOptionsBuilderInfrastructure)
            new H2DbContextOptionsBuilder(new DbContextOptionsBuilder()).UseNetTopologySuite())
        .OptionsBuilder).Options)
{
    #region Database

    [Fact]
    public virtual void CreateDatabaseOperation()
    {
        Generate(new H2CreateDatabaseOperation { Name = "Northwind" });

        AssertSql(
            """
CREATE DATABASE "Northwind";

""");
    }

    [ConditionalFact]
    public virtual void CreateDatabaseOperation_with_collation()
    {
        Generate(
            new H2CreateDatabaseOperation { Name = "Northwind", Collation = "POSIX" });

        AssertSql(
            """
CREATE DATABASE "Northwind"
LC_COLLATE "POSIX";

""");
    }

    [Fact]
    public virtual void CreateDatabaseOperation_with_template()
    {
        Generate(new H2CreateDatabaseOperation { Name = "Northwind", Template = "MyTemplate" });

        AssertSql(
            """
CREATE DATABASE "Northwind"
TEMPLATE "MyTemplate";

""");
    }


    #endregion

    public override void AddColumnOperation_without_column_type()
    {
        base.AddColumnOperation_without_column_type();

        AssertSql(
            """
ALTER TABLE "People" ADD "Alias" varchar NOT NULL;

""");
    }

    public override void AddColumnOperation_with_unicode_overridden()
    {
        base.AddColumnOperation_with_unicode_overridden();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Name" varchar;

""");
    }

    public override void AddColumnOperation_with_unicode_no_model()
    {
        base.AddColumnOperation_with_unicode_no_model();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Name" varchar;

""");
    }

    public override void AddColumnOperation_with_fixed_length_no_model()
    {
        base.AddColumnOperation_with_fixed_length_no_model();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Name" char(100);

""");
    }

    public override void AddColumnOperation_with_maxLength_overridden()
    {
        base.AddColumnOperation_with_maxLength_overridden();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Name" varchar(32);

""");
    }

    public override void AddColumnOperation_with_maxLength_no_model()
    {
        base.AddColumnOperation_with_maxLength_no_model();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Name" varchar(30);

""");
    }

    public override void AddColumnOperation_with_precision_and_scale_overridden()
    {
        base.AddColumnOperation_with_precision_and_scale_overridden();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Pi" numeric(15,10) NOT NULL;

""");
    }

    public override void AddColumnOperation_with_precision_and_scale_no_model()
    {
        base.AddColumnOperation_with_precision_and_scale_no_model();

        AssertSql(
            """
ALTER TABLE "Person" ADD "Pi" numeric(20,7) NOT NULL;

""");
    }

    public override void AddForeignKeyOperation_without_principal_columns()
    {
        base.AddForeignKeyOperation_without_principal_columns();

        AssertSql(
            """
ALTER TABLE "People" ADD FOREIGN KEY ("SpouseId") REFERENCES "People";

""");
    }

    public override void AlterColumnOperation_without_column_type()
    {
        base.AlterColumnOperation_without_column_type();

        AssertSql(
            """
ALTER TABLE "People" ALTER COLUMN "LuckyNumber" TYPE integer;

""");
    }

    public override void RenameTableOperation_legacy()
    {
        base.RenameTableOperation_legacy();

        AssertSql(
            """
ALTER TABLE "dbo"."People" RENAME TO "Person";

""");
    }

    public override void RenameTableOperation()
    {
        base.RenameTableOperation();

        AssertSql(
            """
ALTER TABLE "dbo"."People" RENAME TO "Person";

""");
    }

    public override void SqlOperation()
    {
        base.SqlOperation();

        AssertSql(
            """
-- I <3 DDL

""");
    }

    public override void InsertDataOperation_all_args_spatial()
    {
        base.InsertDataOperation_all_args_spatial();

        AssertSql(
            """
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (0, NULL, NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (1, 'Daenerys Targaryen', NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (2, 'John Snow', NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (3, 'Arya Stark', NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (4, 'Harry Strickland', NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (5, 'The Imp', NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (6, 'The Kingslayer', NULL);
INSERT INTO "dbo"."People" ("Id", "Full Name", "Geometry")
VALUES (7, 'Aemon Targaryen', GEOMETRY 'SRID=4326;GEOMETRYCOLLECTION Z(LINESTRING Z(1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 7.1 7.2 NaN), LINESTRING Z(7.1 7.2 NaN, 20.2 20.2 NaN, 20.2 1.1 NaN, 70.1 70.2 NaN), MULTIPOINT Z((1.1 2.2 NaN), (2.2 2.2 NaN), (2.2 1.1 NaN)), POLYGON Z((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 1.1 2.2 NaN)), POLYGON Z((10.1 20.2 NaN, 20.2 20.2 NaN, 20.2 10.1 NaN, 10.1 20.2 NaN)), POINT Z(1.1 2.2 3.3), MULTILINESTRING Z((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 7.1 7.2 NaN), (7.1 7.2 NaN, 20.2 20.2 NaN, 20.2 1.1 NaN, 70.1 70.2 NaN)), MULTIPOLYGON Z(((10.1 20.2 NaN, 20.2 20.2 NaN, 20.2 10.1 NaN, 10.1 20.2 NaN)), ((1.1 2.2 NaN, 2.2 2.2 NaN, 2.2 1.1 NaN, 1.1 2.2 NaN))))');

""");
    }

    public override void InsertDataOperation_required_args()
    {
        base.InsertDataOperation_required_args();

        AssertSql(
            """
INSERT INTO "dbo"."People" ("First Name")
VALUES ('John');

""");
    }

    public override void InsertDataOperation_required_args_composite()
    {
        base.InsertDataOperation_required_args_composite();

        AssertSql(
            """
INSERT INTO "dbo"."People" ("First Name", "Last Name")
VALUES ('John', 'Snow');

""");
    }

    public override void InsertDataOperation_required_args_multiple_rows()
    {
        base.InsertDataOperation_required_args_multiple_rows();

        AssertSql(
            """
INSERT INTO "dbo"."People" ("First Name")
VALUES ('John');
INSERT INTO "dbo"."People" ("First Name")
VALUES ('Daenerys');

""");
    }

    public override void DeleteDataOperation_all_args()
    {
        base.DeleteDataOperation_all_args();

        AssertSql(
            """
DELETE FROM "People"
WHERE "First Name" = 'Hodor';
DELETE FROM "People"
WHERE "First Name" = 'Daenerys';
DELETE FROM "People"
WHERE "First Name" = 'John';
DELETE FROM "People"
WHERE "First Name" = 'Arya';
DELETE FROM "People"
WHERE "First Name" = 'Harry';

""");
    }

    public override void DeleteDataOperation_all_args_composite()
    {
        base.DeleteDataOperation_all_args_composite();

        AssertSql(
            """
DELETE FROM "People"
WHERE "First Name" = 'Hodor' AND "Last Name" IS NULL;
DELETE FROM "People"
WHERE "First Name" = 'Daenerys' AND "Last Name" = 'Targaryen';
DELETE FROM "People"
WHERE "First Name" = 'John' AND "Last Name" = 'Snow';
DELETE FROM "People"
WHERE "First Name" = 'Arya' AND "Last Name" = 'Stark';
DELETE FROM "People"
WHERE "First Name" = 'Harry' AND "Last Name" = 'Strickland';

""");
    }

    public override void DeleteDataOperation_required_args()
    {
        base.DeleteDataOperation_required_args();

        AssertSql(
            """
DELETE FROM "People"
WHERE "Last Name" = 'Snow';

""");
    }

    public override void DeleteDataOperation_required_args_composite()
    {
        base.DeleteDataOperation_required_args_composite();

        AssertSql(
            """
DELETE FROM "People"
WHERE "First Name" = 'John' AND "Last Name" = 'Snow';

""");
    }

    public override void UpdateDataOperation_all_args()
    {
        base.UpdateDataOperation_all_args();

        AssertSql(
            """
UPDATE "People" SET "Birthplace" = 'Winterfell', "House Allegiance" = 'Stark', "Culture" = 'Northmen'
WHERE "First Name" = 'Hodor';
UPDATE "People" SET "Birthplace" = 'Dragonstone', "House Allegiance" = 'Targaryen', "Culture" = 'Valyrian'
WHERE "First Name" = 'Daenerys';

""");
    }

    public override void UpdateDataOperation_all_args_composite()
    {
        base.UpdateDataOperation_all_args_composite();

        AssertSql(
            """
UPDATE "People" SET "House Allegiance" = 'Stark'
WHERE "First Name" = 'Hodor' AND "Last Name" IS NULL;
UPDATE "People" SET "House Allegiance" = 'Targaryen'
WHERE "First Name" = 'Daenerys' AND "Last Name" = 'Targaryen';

""");
    }

    public override void UpdateDataOperation_all_args_composite_multi()
    {
        base.UpdateDataOperation_all_args_composite_multi();

        AssertSql(
            """
UPDATE "People" SET "Birthplace" = 'Winterfell', "House Allegiance" = 'Stark', "Culture" = 'Northmen'
WHERE "First Name" = 'Hodor' AND "Last Name" IS NULL;
UPDATE "People" SET "Birthplace" = 'Dragonstone', "House Allegiance" = 'Targaryen', "Culture" = 'Valyrian'
WHERE "First Name" = 'Daenerys' AND "Last Name" = 'Targaryen';

""");
    }

    public override void UpdateDataOperation_all_args_multi()
    {
        base.UpdateDataOperation_all_args_multi();

        AssertSql(
            """
UPDATE "People" SET "Birthplace" = 'Dragonstone', "House Allegiance" = 'Targaryen', "Culture" = 'Valyrian'
WHERE "First Name" = 'Daenerys';

""");
    }

    public override void UpdateDataOperation_required_args()
    {
        base.UpdateDataOperation_required_args();

        AssertSql(
            """
UPDATE "People" SET "House Allegiance" = 'Targaryen'
WHERE "First Name" = 'Daenerys';

""");
    }

    public override void UpdateDataOperation_required_args_multiple_rows()
    {
        base.UpdateDataOperation_required_args_multiple_rows();

        AssertSql(
            """
UPDATE "People" SET "House Allegiance" = 'Stark'
WHERE "First Name" = 'Hodor';
UPDATE "People" SET "House Allegiance" = 'Targaryen'
WHERE "First Name" = 'Daenerys';

""");
    }

    public override void UpdateDataOperation_required_args_composite()
    {
        base.UpdateDataOperation_required_args_composite();

        AssertSql(
            """
UPDATE "People" SET "House Allegiance" = 'Targaryen'
WHERE "First Name" = 'Daenerys' AND "Last Name" = 'Targaryen';

""");
    }

    public override void UpdateDataOperation_required_args_composite_multi()
    {
        base.UpdateDataOperation_required_args_composite_multi();

        AssertSql(
            """
UPDATE "People" SET "Birthplace" = 'Dragonstone', "House Allegiance" = 'Targaryen', "Culture" = 'Valyrian'
WHERE "First Name" = 'Daenerys' AND "Last Name" = 'Targaryen';

""");
    }

    public override void UpdateDataOperation_required_args_multi()
    {
        base.UpdateDataOperation_required_args_multi();

        AssertSql(
            """
UPDATE "People" SET "Birthplace" = 'Dragonstone', "House Allegiance" = 'Targaryen', "Culture" = 'Valyrian'
WHERE "First Name" = 'Daenerys';

""");
    }

    public override void DefaultValue_with_line_breaks(bool isUnicode)
    {
        // https://github.com/npgsql/efcore.pg/issues/1478
    }

    public override void DefaultValue_with_line_breaks_2(bool isUnicode)
    {
        // https://github.com/npgsql/efcore.pg/issues/1478
    }

    public override void Sequence_restart_operation(long? startsAt)
    {
        base.Sequence_restart_operation(startsAt);

        AssertSql(
            startsAt.HasValue
                ? $"""
ALTER SEQUENCE "dbo"."TestRestartSequenceOperation" START WITH {startsAt};
ALTER SEQUENCE "dbo"."TestRestartSequenceOperation" RESTART;
"""
                : """ALTER SEQUENCE "dbo"."TestRestartSequenceOperation" RESTART;""");
    }

    [Theory]
    [InlineData(MigrationsSqlGenerationOptions.Default)]
    [InlineData(MigrationsSqlGenerationOptions.Idempotent)]
    public void Alter_column_change_serial_to_identity_idempotent(MigrationsSqlGenerationOptions options)
    {
        Generate(
            modelBuilder =>
            {
                modelBuilder.HasAnnotation(CoreAnnotationNames.ProductVersion, "3.1.0");
                modelBuilder.Entity<Person>().Property<int>("Id").UseSerialColumn();
            },
            [
                new AlterColumnOperation
                {
                    Table = "Person",
                    Name = "Id",
                    ClrType = typeof(int),
                    [H2AnnotationNames.ValueGenerationStrategy] =
                        H2ValueGenerationStrategy.IdentityByDefaultColumn,
                    OldColumn = new AddColumnOperation
                    {
                        Table = "Person",
                        Name = "Id",
                        ClrType = typeof(int),
                        [H2AnnotationNames.ValueGenerationStrategy] =
                            H2ValueGenerationStrategy.SerialColumn,
                    }
                }
            ],
            options);

        AssertSql(
            $"""
ALTER SEQUENCE "Person_Id_seq" RENAME TO "Person_Id_old_seq";
ALTER TABLE "Person" ALTER COLUMN "Id" DROP DEFAULT;
ALTER TABLE "Person" ALTER COLUMN "Id" ADD GENERATED BY DEFAULT AS IDENTITY;
{(options == MigrationsSqlGenerationOptions.Idempotent ? "PERFORM" : "SELECT")} * FROM setval('"Person_Id_seq"', nextval('"Person_Id_old_seq"'), false);
DROP SEQUENCE "Person_Id_old_seq";

""");
    }

    [Fact]
    public void Create_schema_idempotent()
    {
        Generate(
            _ => { },
            [new EnsureSchemaOperation { Name = "some_schema" }],
            MigrationsSqlGenerationOptions.Idempotent);

        // H2 2.5 has CREATE SCHEMA IF NOT EXISTS, and the reference takes it. The
        // PostgreSQL form of this -- a DO block that checks pg_namespace -- is not
        // H2's: H2 has no DO block, and its catalogue is INFORMATION_SCHEMA.
        AssertSql(
            """
CREATE SCHEMA IF NOT EXISTS "some_schema";

""");    }

    #region CockroachDB interleave-in-parent

    // Note that we don't run tests against actual CockroachDB instances, so these are unit tests asserting on SQL
    // only


    #endregion CockroachDB interleave-in-parent

#pragma warning disable 618
    [Fact]
    public virtual void AddColumnOperation_serial_old_annotation_throws()
        => Assert.Throws<NotSupportedException>(
            () =>
                Generate(
                    new AddColumnOperation
                    {
                        Table = "People",
                        Name = "foo",
                        ClrType = typeof(int),
                        ColumnType = "int",
                        IsNullable = false,
                        [H2AnnotationNames.ValueGeneratedOnAdd] = true
                    }));

    public override void InsertDataOperation_throws_for_unsupported_column_types()
        => Assert.Equal(
            RelationalStrings.UnsupportedDataOperationStoreType("foo", "dbo.People.First Name"),
            Assert.Throws<InvalidOperationException>(
                () =>
                    Generate(
                        new InsertDataOperation
                        {
                            Table = "People",
                            Schema = "dbo",
                            Columns = ["First Name"],
                            ColumnTypes = ["foo"],
                            Values = new object[,] { { null } }
                        })).Message);

#pragma warning restore 618

    protected override string GetGeometryCollectionStoreType()
        => "GEOMETRY(GEOMETRYCOLLECTION)";
}
