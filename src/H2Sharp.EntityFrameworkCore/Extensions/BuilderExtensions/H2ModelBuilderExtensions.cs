using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;
using H2Sharp.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     H2-specific extension methods for <see cref="ModelBuilder" />.
/// </summary>
public static class H2ModelBuilderExtensions
{
    #region HiLo

    /// <summary>
    ///     Configures the model to use a sequence-based hi-lo pattern to generate values for properties
    ///     marked as <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseHiLo(this ModelBuilder modelBuilder, string? name = null, string? schema = null)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
        Check.NullButNotEmpty(name, nameof(name));
        Check.NullButNotEmpty(schema, nameof(schema));

        var model = modelBuilder.Model;

        name ??= H2ModelExtensions.DefaultHiLoSequenceName;

        if (model.FindSequence(name, schema) is null)
        {
            modelBuilder.HasSequence(name, schema).IncrementsBy(10);
        }

        model.SetValueGenerationStrategy(H2ValueGenerationStrategy.SequenceHiLo);
        model.SetHiLoSequenceName(name);
        model.SetHiLoSequenceSchema(schema);
        model.SetSequenceNameSuffix(null);
        model.SetSequenceSchema(null);

        return modelBuilder;
    }

    /// <summary>
    ///     Configures the database sequence used for the hi-lo pattern to generate values for key properties
    ///     marked as <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL.
    /// </summary>
    /// <param name="modelBuilder"> The model builder. </param>
    /// <param name="name"> The name of the sequence. </param>
    /// <param name="schema">The schema of the sequence. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> A builder to further configure the sequence. </returns>
    public static IConventionSequenceBuilder? HasHiLoSequence(
        this IConventionModelBuilder modelBuilder,
        string? name,
        string? schema,
        bool fromDataAnnotation = false)
    {
        if (!modelBuilder.CanSetHiLoSequence(name, schema))
        {
            return null;
        }

        modelBuilder.Metadata.SetHiLoSequenceName(name, fromDataAnnotation);
        modelBuilder.Metadata.SetHiLoSequenceSchema(schema, fromDataAnnotation);

        return name is null ? null : modelBuilder.HasSequence(name, schema, fromDataAnnotation);
    }

    /// <summary>
    ///     Returns a value indicating whether the given name and schema can be set for the hi-lo sequence.
    /// </summary>
    /// <param name="modelBuilder"> The model builder. </param>
    /// <param name="name"> The name of the sequence. </param>
    /// <param name="schema">The schema of the sequence. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> <c>true</c> if the given name and schema can be set for the hi-lo sequence. </returns>
    public static bool CanSetHiLoSequence(
        this IConventionModelBuilder modelBuilder,
        string? name,
        string? schema,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
        Check.NullButNotEmpty(name, nameof(name));
        Check.NullButNotEmpty(schema, nameof(schema));

        return modelBuilder.CanSetAnnotation(H2AnnotationNames.HiLoSequenceName, name, fromDataAnnotation)
            && modelBuilder.CanSetAnnotation(H2AnnotationNames.HiLoSequenceSchema, schema, fromDataAnnotation);
    }

    #endregion HiLo

    #region Serial

    /// <summary>
    ///     <para>
    ///         Configures the model to use the PostgreSQL SERIAL feature to generate values for properties
    ///         marked as <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL.
    ///     </para>
    ///     <para>
    ///         This option should be considered deprecated starting with PostgreSQL 10, consider using <see cref="UseIdentityColumns" /> instead.
    ///     </para>
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseSerialColumns(
        this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        var property = modelBuilder.Model;

        property.SetValueGenerationStrategy(H2ValueGenerationStrategy.SerialColumn);
        property.SetHiLoSequenceName(null);
        property.SetHiLoSequenceSchema(null);

        return modelBuilder;
    }

    #endregion Serial

    #region Identity

    /// <summary>
    ///     <para>
    ///         Configures the model to use the PostgreSQL IDENTITY feature to generate values for properties
    ///         marked as <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL. Values for these
    ///         columns will always be generated as identity, and the application will not be able to override
    ///         this behavior by providing a value.
    ///     </para>
    ///     <para>Available only starting PostgreSQL 10.</para>
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseIdentityAlwaysColumns(this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        var model = modelBuilder.Model;

        model.SetValueGenerationStrategy(H2ValueGenerationStrategy.IdentityAlwaysColumn);
        model.SetSequenceNameSuffix(null);
        model.SetSequenceSchema(null);
        model.SetHiLoSequenceName(null);
        model.SetHiLoSequenceSchema(null);

        return modelBuilder;
    }

    /// <summary>
    ///     <para>
    ///         Configures the model to use the PostgreSQL IDENTITY feature to generate values for properties
    ///         marked as <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL. Values for these
    ///         columns will be generated as identity by default, but the application will be able to override
    ///         this behavior by providing a value.
    ///     </para>
    ///     <para>
    ///         This is the default behavior when targeting PostgreSQL. Available only starting PostgreSQL 10.
    ///     </para>
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseIdentityByDefaultColumns(this ModelBuilder modelBuilder)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        var model = modelBuilder.Model;

        model.SetValueGenerationStrategy(H2ValueGenerationStrategy.IdentityByDefaultColumn);
        model.SetSequenceNameSuffix(null);
        model.SetSequenceSchema(null);
        model.SetHiLoSequenceName(null);
        model.SetHiLoSequenceSchema(null);

        return modelBuilder;
    }

    /// <summary>
    ///     <para>
    ///         Configures the model to use the PostgreSQL IDENTITY feature to generate values for properties
    ///         marked as <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL. Values for these
    ///         columns will be generated as identity by default, but the application will be able to override
    ///         this behavior by providing a value.
    ///     </para>
    ///     <para>
    ///         This is the default behavior when targeting PostgreSQL. Available only starting PostgreSQL 10.
    ///     </para>
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseIdentityColumns(this ModelBuilder modelBuilder)
        => modelBuilder.UseIdentityByDefaultColumns();

    /// <summary>
    ///     Configures the value generation strategy for the key property, when targeting PostgreSQL.
    /// </summary>
    /// <param name="modelBuilder">The builder for the property being configured.</param>
    /// <param name="valueGenerationStrategy">The value generation strategy.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>
    ///     The same builder instance if the configuration was applied, <c>null</c> otherwise.
    /// </returns>
    public static IConventionModelBuilder? HasValueGenerationStrategy(
        this IConventionModelBuilder modelBuilder,
        H2ValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
    {
        if (modelBuilder.CanSetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation))
        {
            modelBuilder.Metadata.SetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);

            if (valueGenerationStrategy != H2ValueGenerationStrategy.SequenceHiLo)
            {
                modelBuilder.HasHiLoSequence(null, null, fromDataAnnotation);
            }

            if (valueGenerationStrategy != H2ValueGenerationStrategy.Sequence)
            {
                if (modelBuilder.CanSetAnnotation(H2AnnotationNames.SequenceNameSuffix, null)
                    && modelBuilder.CanSetAnnotation(H2AnnotationNames.SequenceSchema, null))
                {
                    modelBuilder.Metadata.SetSequenceNameSuffix(null, fromDataAnnotation);
                    modelBuilder.Metadata.SetSequenceSchema(null, fromDataAnnotation);
                }
            }

            return modelBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the given value can be set as the default value generation strategy.
    /// </summary>
    /// <param name="modelBuilder"> The model builder. </param>
    /// <param name="valueGenerationStrategy"> The value generation strategy. </param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation. </param>
    /// <returns> <c>true</c> if the given value can be set as the default value generation strategy. </returns>
    public static bool CanSetValueGenerationStrategy(
        this IConventionModelBuilder modelBuilder,
        H2ValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));

        return modelBuilder.CanSetAnnotation(
            H2AnnotationNames.ValueGenerationStrategy, valueGenerationStrategy, fromDataAnnotation);
    }

    #endregion Identity

    #region Sequence

    /// <summary>
    ///     Configures the model to use a sequence per hierarchy to generate values for key properties marked as
    ///     <see cref="ValueGenerated.OnAdd" />, when targeting PostgreSQL.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="nameSuffix">The name that will suffix the table name for each sequence created automatically.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static ModelBuilder UseKeySequences(
        this ModelBuilder modelBuilder,
        string? nameSuffix = null,
        string? schema = null)
    {
        Check.NullButNotEmpty(nameSuffix, nameof(nameSuffix));
        Check.NullButNotEmpty(schema, nameof(schema));

        var model = modelBuilder.Model;

        nameSuffix ??= H2ModelExtensions.DefaultSequenceNameSuffix;

        model.SetValueGenerationStrategy(H2ValueGenerationStrategy.Sequence);
        model.SetSequenceNameSuffix(nameSuffix);
        model.SetSequenceSchema(schema);
        model.SetHiLoSequenceName(null);
        model.SetHiLoSequenceSchema(null);

        return modelBuilder;
    }

    #endregion Sequence


    #region Enums

    /// <summary>
    ///     Registers a user-defined enum type in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder in which to create the enum type.</param>
    /// <param name="schema">The schema in which to create the enum type.</param>
    /// <param name="name">The name of the enum type to create.</param>
    /// <param name="labels">The enum label values.</param>
    /// <returns>
    ///     The updated <see cref="ModelBuilder" />.
    /// </returns>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/current/static/datatype-enum.html
    /// </remarks>
    /// <exception cref="ArgumentNullException">builder</exception>
    public static ModelBuilder HasH2Enum(
        this ModelBuilder modelBuilder,
        string? schema,
        string name,
        string[] labels)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
        Check.NotEmpty(name, nameof(name));
        Check.NotNull(labels, nameof(labels));

        modelBuilder.Model.GetOrAddH2Enum(schema, name, labels);
        return modelBuilder;
    }

    /// <summary>
    ///     Registers a user-defined enum type in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder in which to create the enum type.</param>
    /// <param name="schema">The schema in which to create the enum type.</param>
    /// <param name="name">The name of the enum type to create.</param>
    /// <param name="labels">The enum label values.</param>
    /// <returns>
    ///     The updated <see cref="ModelBuilder" />.
    /// </returns>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/current/static/datatype-enum.html
    /// </remarks>
    /// <exception cref="ArgumentNullException">builder</exception>
    public static IConventionModelBuilder HasH2Enum(
        this IConventionModelBuilder modelBuilder,
        string? schema,
        string name,
        string[] labels)
    {
        Check.NotNull(modelBuilder, nameof(modelBuilder));
        Check.NotEmpty(name, nameof(name));
        Check.NotNull(labels, nameof(labels));

        if (modelBuilder.CanSetH2Enum(schema, name))
        {
            modelBuilder.Metadata.GetOrAddH2Enum(schema, name, labels);
        }
        return modelBuilder;
    }

    /// <summary>
    ///     Returns a value indicating whether the given PostgreSQL extension can be registered in the model.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see>, and
    ///     <see href="https://aka.ms/efcore-docs-sqlserver">Accessing SQL Server and SQL Azure databases with EF Core</see>
    ///     for more information and examples.
    /// </remarks>
    /// <param name="modelBuilder">The model builder.</param>
    /// <param name="schema">The schema in which to create the extension.</param>
    /// <param name="name">The name of the extension to create.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true" /> if the given value can be set as the default increment for SQL Server IDENTITY.</returns>
    public static bool CanSetH2Enum(
        this IConventionModelBuilder modelBuilder,
        string? schema,
        string name,
        bool fromDataAnnotation = false)
    {
        var annotationName = H2Enum.BuildAnnotationName(schema, name);

        return modelBuilder.CanSetAnnotation(annotationName, $"{schema},{name}", fromDataAnnotation);
    }

    /// <summary>
    ///     Registers a user-defined enum type in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder in which to create the enum type.</param>
    /// <param name="name">The name of the enum type to create.</param>
    /// <param name="labels">The enum label values.</param>
    /// <returns>
    ///     The updated <see cref="ModelBuilder" />.
    /// </returns>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/current/static/datatype-enum.html
    /// </remarks>
    /// <exception cref="ArgumentNullException">builder</exception>
    public static ModelBuilder HasH2Enum(
        this ModelBuilder modelBuilder,
        string name,
        string[] labels)
        => modelBuilder.HasH2Enum(null, name, labels);

    /// <summary>
    ///     Registers a user-defined enum type in the model.
    /// </summary>
    /// <param name="modelBuilder">The model builder in which to create the enum type.</param>
    /// <param name="schema">The schema in which to create the enum type.</param>
    /// <param name="name">The name of the enum type to create.</param>
    /// <param name="nameTranslator">
    ///     The translator for name and label inference.
    ///     Defaults to <see cref="H2UpperCaseNameTranslator" />.
    /// </param>
    /// <typeparam name="TEnum"></typeparam>
    /// <returns>
    ///     The updated <see cref="ModelBuilder" />.
    /// </returns>
    /// <remarks>
    ///     See: https://www.postgresql.org/docs/current/static/datatype-enum.html
    /// </remarks>
    /// <exception cref="ArgumentNullException">builder</exception>
    public static ModelBuilder HasH2Enum<TEnum>(
        this ModelBuilder modelBuilder,
        string? schema = null,
        string? name = null,
        IH2NameTranslator? nameTranslator = null)
        where TEnum : struct, Enum
    {
#pragma warning disable CS0618 // H2Connection.GlobalTypeMapper is obsolete
        nameTranslator ??= H2UpperCaseNameTranslator.Instance;
#pragma warning restore CS0618

        return modelBuilder.HasH2Enum(
            schema,
            name ?? GetTypeStoreName<TEnum>(nameTranslator),
            GetMemberStoreNames<TEnum>(nameTranslator));
    }

    #endregion

    #region Templates

    // H2 has no template database. PostgreSQL's CREATE DATABASE ... TEMPLATE copies another
    // database; H2 creates the database empty and has no equivalent of that clause, so there is
    // nothing to configure here.

    #endregion




    #region Helpers

    // See: https://github.com/npgsql/npgsql/blob/dev/src/H2/TypeMapping/TypeMapperBase.cs#L132-L138
    private static string GetTypeStoreName<TEnum>(IH2NameTranslator nameTranslator)
        where TEnum : struct, Enum
        => typeof(TEnum).GetCustomAttribute<ColumnAttribute>()?.Name ?? nameTranslator.TranslateTypeName(typeof(TEnum).Name);

    // See: https://github.com/npgsql/npgsql/blob/dev/src/H2/TypeHandlers/EnumHandler.cs#L118-L129
    private static string[] GetMemberStoreNames<TEnum>(IH2NameTranslator nameTranslator)
        where TEnum : struct, Enum
        => typeof(TEnum)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(x => x.GetCustomAttribute<ColumnAttribute>()?.Name ?? nameTranslator.TranslateMemberName(x.Name))
            .ToArray();

    #endregion
}
