using Microsoft.EntityFrameworkCore.Storage.Json;
using H2Sharp.EntityFrameworkCore.Internal;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Conventions;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.Design.Internal;

public class H2AnnotationCodeGeneratorTest
{
    #region Identity / sequence / HiLo

    [Fact]
    public void GenerateFluentApi_value_generation()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.Entity(
            "Post",
            x =>
            {
                x.Property<int>("IdentityByDefault").UseIdentityByDefaultColumn();
                x.Property<int>("IdentityAlways").UseIdentityAlwaysColumn();
                x.Property<int>("Serial").UseSerialColumn();
                x.Property<int>("None").Metadata.SetValueGenerationStrategy(H2ValueGenerationStrategy.None);
            });

        // Note: both serial and identity-by-default columns are considered by-convention - we don't want
        // to assume that the PostgreSQL version of the scaffolded database necessarily determines the
        // version of the database that the scaffolded model will target. This makes life difficult for
        // models with mixed strategies but that's an edge case.

        var entity = (IEntityType)modelBuilder.Model.FindEntityType("Post");

        var property = entity.GetProperties().Single(p => p.Name == "IdentityByDefault");
        var annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        generator.RemoveAnnotationsHandledByConventions(property, annotations);
        Assert.Empty(annotations);
        var result = generator.GenerateFluentApiCalls(property, property.GetAnnotations().ToDictionary(a => a.Name, a => a))
            .Single();
        Assert.Equal(nameof(H2PropertyBuilderExtensions.UseIdentityByDefaultColumn), result.Method);
        Assert.Empty(result.Arguments);

        property = entity.GetProperties().Single(p => p.Name == "IdentityAlways");
        annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        generator.RemoveAnnotationsHandledByConventions(property, annotations);
        Assert.Contains(annotations, kv => kv.Key == H2AnnotationNames.ValueGenerationStrategy);
        result = generator.GenerateFluentApiCalls(property, annotations).Single();
        Assert.Equal(nameof(H2PropertyBuilderExtensions.UseIdentityAlwaysColumn), result.Method);
        Assert.Empty(result.Arguments);

        property = entity.GetProperties().Single(p => p.Name == "Serial");
        annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        generator.RemoveAnnotationsHandledByConventions(property, annotations);
        Assert.Empty(annotations);
        result = generator.GenerateFluentApiCalls(property, property.GetAnnotations().ToDictionary(a => a.Name, a => a))
            .Single();
        Assert.Equal(nameof(H2PropertyBuilderExtensions.UseSerialColumn), result.Method);
        Assert.Empty(result.Arguments);

        property = entity.GetProperties().Single(p => p.Name == "None");
        annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        generator.RemoveAnnotationsHandledByConventions(property, annotations);
        Assert.Contains(annotations, kv => kv.Key == H2AnnotationNames.ValueGenerationStrategy);
        result = generator.GenerateFluentApiCalls(property, property.GetAnnotations().ToDictionary(a => a.Name, a => a))
            .Single();
        Assert.Equal(nameof(PropertyBuilder.HasAnnotation), result.Method);
        Assert.Collection(
            result.Arguments,
            a => Assert.Equal(H2AnnotationNames.ValueGenerationStrategy, a),
            a => Assert.Equal(H2ValueGenerationStrategy.None, a));
    }

    [Fact]
    public void GenerateFluentApi_identity_sequence_options()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.Entity(
            "Post",
            x =>
            {
                x.Property<int>("Id")
                    .UseIdentityByDefaultColumn()
                    .HasIdentityOptions(
                        startValue: 5,
                        incrementBy: 2,
                        minValue: 3,
                        maxValue: 2000,
                        cyclic: true,
                        numbersToCache: 10);
            });

        var property = (IProperty)modelBuilder.Model.FindEntityType("Post").GetProperties()
            .Single(p => p.Name == "Id");
        var annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        generator.RemoveAnnotationsHandledByConventions(property, annotations);
        Assert.Contains(annotations, kv => kv.Key == H2AnnotationNames.IdentityOptions);
        var result = generator.GenerateFluentApiCalls(property, annotations).Single();
        Assert.Equal(nameof(H2PropertyBuilderExtensions.HasIdentityOptions), result.Method);
        Assert.Equal(5L, result.Arguments[0]);
        Assert.Equal(2L, result.Arguments[1]);
        Assert.Equal(3L, result.Arguments[2]);
        Assert.Equal(2000L, result.Arguments[3]);
        Assert.Equal(true, result.Arguments[4]);
        Assert.Equal(10L, result.Arguments[5]);
    }

    [ConditionalFact]
    public void GenerateFluentApi_IModel_works_with_IdentityByDefault()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.UseIdentityByDefaultColumns();

        var annotations = modelBuilder.Model.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(((IModel)modelBuilder.Model), annotations).Single();

        Assert.Equal("UseIdentityByDefaultColumns", result.Method);
        Assert.Equal("H2ModelBuilderExtensions", result.DeclaringType);

        Assert.Empty(result.Arguments);
    }

    [ConditionalFact]
    public void GenerateFluentApi_IProperty_works_with_IdentityByDefault()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.Entity("Post", x => x.Property<int>("Id").UseIdentityByDefaultColumn());
        var property = (IProperty)modelBuilder.Model.FindEntityType("Post").FindProperty("Id");

        var annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(property, annotations).Single();

        Assert.Equal("UseIdentityByDefaultColumn", result.Method);
        Assert.Equal("H2PropertyBuilderExtensions", result.DeclaringType);

        Assert.Empty(result.Arguments);
    }

    [ConditionalFact]
    public void GenerateFluentApi_IModel_works_with_IdentityAlways()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.UseIdentityAlwaysColumns();

        var annotations = modelBuilder.Model.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(((IModel)modelBuilder.Model), annotations).Single();

        Assert.Equal("UseIdentityAlwaysColumns", result.Method);
        Assert.Equal("H2ModelBuilderExtensions", result.DeclaringType);

        Assert.Empty(result.Arguments);
    }

    [ConditionalFact]
    public void GenerateFluentApi_IProperty_works_with_IdentityAlways()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.Entity("Post", x => x.Property<int>("Id").UseIdentityAlwaysColumn());
        var property = (IProperty)modelBuilder.Model.FindEntityType("Post").FindProperty("Id");

        var annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(property, annotations).Single();

        Assert.Equal("UseIdentityAlwaysColumn", result.Method);
        Assert.Equal("H2PropertyBuilderExtensions", result.DeclaringType);

        Assert.Empty(result.Arguments);
    }

    [ConditionalFact]
    public void GenerateFluentApi_IModel_works_with_HiLo()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.UseHiLo("HiLoIndexName", "HiLoIndexSchema");

        var annotations = modelBuilder.Model.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls((IModel)modelBuilder.Model, annotations).Single();

        Assert.Equal("UseHiLo", result.Method);
        Assert.Equal("H2ModelBuilderExtensions", result.DeclaringType);

        Assert.Collection(
            result.Arguments,
            name => Assert.Equal("HiLoIndexName", name),
            schema => Assert.Equal("HiLoIndexSchema", schema));
    }

    [ConditionalFact]
    public void GenerateFluentApi_IProperty_works_with_HiLo()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());
        modelBuilder.Entity("Post", x => x.Property<int>("Id").UseHiLo("HiLoIndexName", "HiLoIndexSchema"));
        var property = (IProperty)modelBuilder.Model.FindEntityType("Post").FindProperty("Id");

        var annotations = property.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(property, annotations).Single();

        Assert.Equal("UseHiLo", result.Method);
        Assert.Equal("H2PropertyBuilderExtensions", result.DeclaringType);

        Assert.Collection(
            result.Arguments,
            name => Assert.Equal("HiLoIndexName", name),
            schema => Assert.Equal("HiLoIndexSchema", schema));
    }

    #endregion Identity / sequence / HiLo

    #region Enum

    [ConditionalFact]
    public void Enum()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());

        var enumLabels = new[] { "someValue1", "someValue2" };
        modelBuilder.HasH2Enum("some_enum", enumLabels);

        var model = (IModel)modelBuilder.Model;
        var annotations = model.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(model, annotations)
            .Single(c => c.Method == nameof(H2ModelBuilderExtensions.HasH2Enum));

        Assert.Collection(
            result.Arguments,
            name => Assert.Equal("some_enum", name),
            labels => Assert.Equal(enumLabels, labels));
    }

    [ConditionalFact]
    public void Enum_with_schema()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());

        var enumLabels = new[] { "someValue1", "someValue2" };
        modelBuilder.HasH2Enum("some_schema", "some_enum", enumLabels);

        var model = (IModel)modelBuilder.Model;
        var annotations = model.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(model, annotations)
            .Single(c => c.Method == nameof(H2ModelBuilderExtensions.HasH2Enum));

        Assert.Collection(
            result.Arguments,
            schema => Assert.Equal("some_schema", schema),
            name => Assert.Equal("some_enum", name),
            labels => Assert.Equal(enumLabels, labels));
    }

    [ConditionalFact]
    public void Enum_with_null_schema()
    {
        var generator = CreateGenerator();
        var modelBuilder = new ModelBuilder(H2ConventionSetBuilder.Build());

        var enumLabels = new[] { "someValue1", "someValue2" };
        modelBuilder.HasH2Enum(schema: null, "some_enum", enumLabels);

        var model = (IModel)modelBuilder.Model;
        var annotations = model.GetAnnotations().ToDictionary(a => a.Name, a => a);
        var result = generator.GenerateFluentApiCalls(model, annotations)
            .Single(c => c.Method == nameof(H2ModelBuilderExtensions.HasH2Enum));

        Assert.Collection(
            result.Arguments,
            name => Assert.Equal("some_enum", name),
            labels => Assert.Equal(enumLabels, labels));
    }

    #endregion Enum

    private H2AnnotationCodeGenerator CreateGenerator()
        => new(
            new AnnotationCodeGeneratorDependencies(
                new H2TypeMappingSource(
                    new TypeMappingSourceDependencies(
                        new ValueConverterSelector(new ValueConverterSelectorDependencies()),
                        new JsonValueReaderWriterSource(new JsonValueReaderWriterSourceDependencies()),
                        []
                    ),
                    new RelationalTypeMappingSourceDependencies([]),
                    new H2SqlGenerationHelper(new RelationalSqlGenerationHelperDependencies()),
                    new H2SingletonOptions())));
}
