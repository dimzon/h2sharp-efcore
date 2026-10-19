using Microsoft.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Metadata.Conventions;

public class H2ValueGenerationStrategyConventionTest
{
    [Fact]
    public void Annotations_are_added_when_conventional_model_builder_is_used()
    {
        var model = H2TestHelpers.Instance.CreateConventionBuilder().Model;

        var annotations = model.GetAnnotations().OrderBy(a => a.Name).ToList();
        Assert.Equal(3, annotations.Count);

        Assert.Equal(H2AnnotationNames.ValueGenerationStrategy, annotations.First().Name);
        Assert.Equal(H2ValueGenerationStrategy.IdentityByDefaultColumn, annotations.First().Value);
    }

    [Fact]
    public void Annotations_are_added_when_conventional_model_builder_is_used_with_sequences()
    {
        var model = H2TestHelpers.Instance.CreateConventionBuilder()
            .UseHiLo()
            .Model;

        model.RemoveAnnotation(CoreAnnotationNames.ProductVersion);

        var annotations = model.GetAnnotations().OrderBy(a => a.Name).ToList();
        Assert.Equal(4, annotations.Count);

        Assert.Equal(H2AnnotationNames.HiLoSequenceName, annotations[0].Name);
        Assert.Equal(H2ModelExtensions.DefaultHiLoSequenceName, annotations[0].Value);

        Assert.Equal(H2AnnotationNames.ValueGenerationStrategy, annotations[1].Name);
        Assert.Equal(H2ValueGenerationStrategy.SequenceHiLo, annotations[1].Value);

        Assert.Equal(RelationalAnnotationNames.MaxIdentifierLength, annotations[2].Name);

        Assert.Equal(
            RelationalAnnotationNames.Sequences,
            annotations[3].Name);
        Assert.NotNull(annotations[3].Value);
    }
}
