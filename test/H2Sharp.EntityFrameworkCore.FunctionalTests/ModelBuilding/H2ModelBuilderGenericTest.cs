using Xunit.Sdk;

namespace H2Sharp.EntityFrameworkCore.ModelBuilding;

#nullable enable

public class H2ModelBuilderGenericTest : H2ModelBuilderTestBase
{
    public class H2GenericNonRelationship(H2ModelBuilderFixture fixture) : H2NonRelationship(fixture)
    {
        // PostgreSQL actually does support mapping multi-dimensional arrays, so no exception is thrown as expected
        protected override void Mapping_throws_for_non_ignored_three_dimensional_array()
            => Assert.Throws<ThrowsException>(() => base.Mapping_throws_for_non_ignored_three_dimensional_array());

        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericComplexType(H2ModelBuilderFixture fixture) : H2ComplexType(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericInheritance(H2ModelBuilderFixture fixture) : H2Inheritance(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericOneToMany(H2ModelBuilderFixture fixture) : H2OneToMany(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericManyToOne(H2ModelBuilderFixture fixture) : H2ManyToOne(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericOneToOne(H2ModelBuilderFixture fixture) : H2OneToOne(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericManyToMany(H2ModelBuilderFixture fixture) : H2ManyToMany(fixture)
    {
        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }

    public class H2GenericOwnedTypes(H2ModelBuilderFixture fixture) : H2OwnedTypes(fixture)
    {
        // PostgreSQL stored procedures do not support result columns
        public override void Can_use_sproc_mapping_with_owned_reference()
            => Assert.Throws<InvalidOperationException>(() => base.Can_use_sproc_mapping_with_owned_reference());

        protected override TestModelBuilder CreateModelBuilder(
            Action<ModelConfigurationBuilder>? configure)
            => new GenericTestModelBuilder(Fixture, configure);
    }
}
