#nullable enable

using Microsoft.EntityFrameworkCore.ModelBuilding;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.ModelBuilding;

public class H2ModelBuilderTestBase : RelationalModelBuilderTest
{
    public abstract class H2NonRelationship(H2ModelBuilderFixture fixture)
        : RelationalNonRelationshipTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2ComplexType(H2ModelBuilderFixture fixture)
        : RelationalComplexTypeTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2Inheritance(H2ModelBuilderFixture fixture)
        : RelationalInheritanceTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2OneToMany(H2ModelBuilderFixture fixture)
        : RelationalOneToManyTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2ManyToOne(H2ModelBuilderFixture fixture)
        : RelationalManyToOneTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2OneToOne(H2ModelBuilderFixture fixture)
        : RelationalOneToOneTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2ManyToMany(H2ModelBuilderFixture fixture)
        : RelationalManyToManyTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public abstract class H2OwnedTypes(H2ModelBuilderFixture fixture)
        : RelationalOwnedTypesTestBase(fixture), IClassFixture<H2ModelBuilderFixture>;

    public class H2ModelBuilderFixture : RelationalModelBuilderFixture
    {
        public override TestHelpers TestHelpers
            => H2TestHelpers.Instance;
    }
}
