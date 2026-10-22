using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore.Metadata.Conventions;

/// <summary>
///     A convention that sets up the enums of H2 2.5, which are declared in the context options.
/// </summary>
/// <remarks>
///     See <see href="https://aka.ms/efcore-docs-conventions">Model building conventions</see>.
/// </remarks>
public class H2PostgresModelFinalizingConvention : IModelFinalizingConvention
{
    private readonly IReadOnlyList<EnumDefinition> _enumDefinitions;

    /// <summary>
    ///     Creates a new instance of <see cref="H2PostgresModelFinalizingConvention" />.
    /// </summary>
    /// <param name="enumDefinitions">The enums to declare in the model.</param>
    public H2PostgresModelFinalizingConvention(
        IReadOnlyList<EnumDefinition> enumDefinitions)
    {
        _enumDefinitions = enumDefinitions;
    }

    /// <inheritdoc />
    public virtual void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
    {
        SetupEnums(modelBuilder);
    }

    /// <summary>
    ///     Configures the model to create the enums of H2 2.5 based on the user's enum definitions in the context options.
    /// </summary>
    protected virtual void SetupEnums(IConventionModelBuilder modelBuilder)
    {
        foreach (var enumDefinition in _enumDefinitions)
        {
            modelBuilder.HasPostgresEnum(
                enumDefinition.StoreTypeSchema,
                enumDefinition.StoreTypeName,
                enumDefinition.Labels.Values.Order(StringComparer.Ordinal).ToArray());
        }
    }
}
