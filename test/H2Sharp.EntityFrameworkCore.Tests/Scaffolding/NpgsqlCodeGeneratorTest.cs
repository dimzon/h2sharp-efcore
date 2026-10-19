using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.NetTopologySuite.Scaffolding.Internal;
using H2Sharp.EntityFrameworkCore.NodaTime.Scaffolding.Internal;
using H2Sharp.EntityFrameworkCore.Scaffolding.Internal;

namespace H2Sharp.EntityFrameworkCore.Scaffolding;

public class NpgsqlCodeGeneratorTest
{
    [Fact]
    public virtual void Use_provider_method_is_generated_correctly()
    {
        var codeGenerator = new NpgsqlCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                []));

        var result = codeGenerator.GenerateUseProvider("Server=test;Username=test;Password=test;Database=test", providerOptions: null);

        Assert.Equal("UseH2", result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal("Server=test;Username=test;Password=test;Database=test", a));
        Assert.Null(result.ChainedCall);
    }

    [Fact]
    public virtual void Use_provider_method_is_generated_correctly_with_options()
    {
        var codeGenerator = new NpgsqlCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                []));

        var providerOptions = new MethodCallCodeFragment(_setProviderOptionMethodInfo);

        var result = codeGenerator.GenerateUseProvider("Server=test;Username=test;Password=test;Database=test", providerOptions);

        Assert.Equal("UseH2", result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal("Server=test;Username=test;Password=test;Database=test", a),
            a =>
            {
                var nestedClosure = Assert.IsType<NestedClosureCodeFragment>(a);

                Assert.Equal("x", nestedClosure.Parameter);
                Assert.Same(providerOptions, nestedClosure.MethodCalls[0]);
            });
        Assert.Null(result.ChainedCall);
    }

    [ConditionalFact]
    public virtual void Use_provider_method_is_generated_correctly_with_NetTopologySuite()
    {
        var codeGenerator = new NpgsqlCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                [new NpgsqlNetTopologySuiteCodeGeneratorPlugin()]));

        var result = ((IProviderConfigurationCodeGenerator)codeGenerator).GenerateUseProvider("Data Source=Test");

        Assert.Equal("UseH2", result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal("Data Source=Test", a),
            a =>
            {
                var nestedClosure = Assert.IsType<NestedClosureCodeFragment>(a);

                Assert.Equal("x", nestedClosure.Parameter);
                Assert.Equal("UseNetTopologySuite", nestedClosure.MethodCalls[0].Method);
            });
        Assert.Null(result.ChainedCall);
    }

    [ConditionalFact]
    public virtual void Use_provider_method_is_generated_correctly_with_NodaTime()
    {
        var codeGenerator = new NpgsqlCodeGenerator(
            new ProviderCodeGeneratorDependencies(
                [new NpgsqlNodaTimeCodeGeneratorPlugin()]));

        var result = ((IProviderConfigurationCodeGenerator)codeGenerator).GenerateUseProvider("Data Source=Test");

        Assert.Equal("UseH2", result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal("Data Source=Test", a),
            a =>
            {
                var nestedClosure = Assert.IsType<NestedClosureCodeFragment>(a);

                Assert.Equal("x", nestedClosure.Parameter);
                Assert.Equal("UseNodaTime", nestedClosure.MethodCalls[0].Method);
            });
        Assert.Null(result.ChainedCall);
    }

    private static readonly MethodInfo _setProviderOptionMethodInfo
        = typeof(NpgsqlCodeGeneratorTest).GetRuntimeMethod(nameof(SetProviderOption), [typeof(DbContextOptionsBuilder)]);

    public static H2DbContextOptionsBuilder SetProviderOption(DbContextOptionsBuilder optionsBuilder)
        => throw new NotSupportedException();
}
