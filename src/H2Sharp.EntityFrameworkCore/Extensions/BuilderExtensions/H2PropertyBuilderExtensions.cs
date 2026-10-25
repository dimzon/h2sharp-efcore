using System.Collections;
using H2Sharp.EntityFrameworkCore.Metadata;
using H2Sharp.EntityFrameworkCore.Metadata.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     H2 specific extension methods for <see cref="PropertyBuilder" />.
/// </summary>
public static class H2PropertyBuilderExtensions
{
    #region HiLo

    /// <summary>
    ///     Configures the property to use a sequence-based hi-lo pattern to generate values for new entities,
    ///     when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <param name="name"> The name of the sequence.</param>
    /// <param name="schema"> The schema of the sequence.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseHiLo(
        this PropertyBuilder propertyBuilder,
        string? name = null,
        string? schema = null)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));
        Check.NullButNotEmpty(name, nameof(name));
        Check.NullButNotEmpty(schema, nameof(schema));

        var property = propertyBuilder.Metadata;

        name ??= H2ModelExtensions.DefaultHiLoSequenceName;

        var model = property.DeclaringType.Model;

        if (model.FindSequence(name, schema) is null)
        {
            model.AddSequence(name, schema).IncrementBy = 10;
        }

        property.SetValueGenerationStrategy(H2ValueGenerationStrategy.SequenceHiLo);
        property.SetHiLoSequenceName(name);
        property.SetHiLoSequenceSchema(schema);
        property.SetSequenceName(null);
        property.SetSequenceSchema(null);
        property.RemoveIdentityOptions();

        return propertyBuilder;
    }

    /// <summary>
    ///     Configures the property to use a sequence-based hi-lo pattern to generate values for new entities,
    ///     when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <param name="name"> The name of the sequence.</param>
    /// <param name="schema"> The schema of the sequence.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseHiLo<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        string? name = null,
        string? schema = null)
        => (PropertyBuilder<TProperty>)UseHiLo((PropertyBuilder)propertyBuilder, name, schema);

    /// <summary>
    ///     Configures the database sequence used for the hi-lo pattern to generate values for the key property,
    ///     when targeting H2.
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <param name="name"> The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the sequence.</returns>
    public static IConventionSequenceBuilder? HasHiLoSequence(
        this IConventionPropertyBuilder propertyBuilder,
        string? name,
        string? schema,
        bool fromDataAnnotation = false)
    {
        if (!propertyBuilder.CanSetHiLoSequence(name, schema))
        {
            return null;
        }

        propertyBuilder.Metadata.SetHiLoSequenceName(name, fromDataAnnotation);
        propertyBuilder.Metadata.SetHiLoSequenceSchema(schema, fromDataAnnotation);

        return name is null
            ? null
            : propertyBuilder.Metadata.DeclaringType.Model.Builder.HasSequence(name, schema, fromDataAnnotation);
    }

    /// <summary>
    ///     Returns a value indicating whether the given name and schema can be set for the hi-lo sequence.
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <param name="name"> The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <param name="fromDataAnnotation"> Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><c>true</c> if the given name and schema can be set for the hi-lo sequence.</returns>
    public static bool CanSetHiLoSequence(
        this IConventionPropertyBuilder propertyBuilder,
        string? name,
        string? schema,
        bool fromDataAnnotation = false)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));
        Check.NullButNotEmpty(name, nameof(name));
        Check.NullButNotEmpty(schema, nameof(schema));

        return propertyBuilder.CanSetAnnotation(H2AnnotationNames.HiLoSequenceName, name, fromDataAnnotation)
            && propertyBuilder.CanSetAnnotation(H2AnnotationNames.HiLoSequenceSchema, schema, fromDataAnnotation);
    }

    #endregion HiLo

    #region Sequence

    /// <summary>
    ///     Configures the key property to use a sequence-based key value generation pattern to generate values for new entities,
    ///     when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseSequence(
        this PropertyBuilder propertyBuilder,
        string? name = null,
        string? schema = null)
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NullButNotEmpty(schema, nameof(schema));

        var property = propertyBuilder.Metadata;

        property.SetValueGenerationStrategy(H2ValueGenerationStrategy.Sequence);
        property.SetSequenceName(name);
        property.SetSequenceSchema(schema);
        property.SetHiLoSequenceName(null);
        property.SetHiLoSequenceSchema(null);

        return propertyBuilder;
    }

    /// <summary>
    ///     Configures the key property to use a sequence-based key value generation pattern to generate values for new entities,
    ///     when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    /// </summary>
    /// <remarks>
    ///     See <see href="https://aka.ms/efcore-docs-modeling">Modeling entity types and relationships</see> for more
    ///     information and examples.
    /// </remarks>
    /// <typeparam name="TProperty">The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseSequence<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        string? name = null,
        string? schema = null)
        => (PropertyBuilder<TProperty>)UseSequence((PropertyBuilder)propertyBuilder, name, schema);

    /// <summary>
    ///     Configures the database sequence used for the key value generation pattern to generate values for the key property,
    ///     when targeting H2.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>A builder to further configure the sequence.</returns>
    public static IConventionSequenceBuilder? HasSequence(
        this IConventionPropertyBuilder propertyBuilder,
        string? name,
        string? schema,
        bool fromDataAnnotation = false)
    {
        if (!propertyBuilder.CanSetSequence(name, schema, fromDataAnnotation))
        {
            return null;
        }

        propertyBuilder.Metadata.SetSequenceName(name, fromDataAnnotation);
        propertyBuilder.Metadata.SetSequenceSchema(schema, fromDataAnnotation);

        return name == null
            ? null
            : propertyBuilder.Metadata.DeclaringType.Model.Builder.HasSequence(name, schema, fromDataAnnotation);
    }

    /// <summary>
    ///     Returns a value indicating whether the given name and schema can be set for the key value generation sequence.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="name">The name of the sequence.</param>
    /// <param name="schema">The schema of the sequence.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true" /> if the given name and schema can be set for the key value generation sequence.</returns>
    public static bool CanSetSequence(
        this IConventionPropertyBuilder propertyBuilder,
        string? name,
        string? schema,
        bool fromDataAnnotation = false)
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NullButNotEmpty(schema, nameof(schema));

        return propertyBuilder.CanSetAnnotation(H2AnnotationNames.SequenceName, name, fromDataAnnotation)
            && propertyBuilder.CanSetAnnotation(H2AnnotationNames.SequenceSchema, schema, fromDataAnnotation);
    }

    #endregion Sequence

    #region Serial

    /// <summary>
    ///     <para>
    ///         Configures the property to have values generated for new entities by the database. H2 has no
    ///         <c>serial</c> pseudo-type: this legacy name writes the same <c>GENERATED BY DEFAULT AS IDENTITY</c>
    ///         column <see cref="UseIdentityByDefaultColumn" /> does, and exists so that a model written for
    ///         PostgreSQL keeps working. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///     </para>
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseSerialColumn(
        this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        var property = propertyBuilder.Metadata;
        property.SetValueGenerationStrategy(H2ValueGenerationStrategy.SerialColumn);
        property.SetSequenceName(null);
        property.SetSequenceSchema(null);
        property.RemoveHiLoOptions();
        property.RemoveIdentityOptions();

        return propertyBuilder;
    }

    /// <summary>
    ///     <para>
    ///         Configures the property to have values generated for new entities by the database. H2 has no
    ///         <c>serial</c> pseudo-type: this legacy name writes the same <c>GENERATED BY DEFAULT AS IDENTITY</c>
    ///         column <see cref="UseIdentityByDefaultColumn" /> does, and exists so that a model written for
    ///         PostgreSQL keeps working. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///     </para>
    /// </summary>
    /// <typeparam name="TProperty"> The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseSerialColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder)
        => (PropertyBuilder<TProperty>)UseSerialColumn((PropertyBuilder)propertyBuilder);

    #endregion Serial

    #region Identity always

    /// <summary>
    ///     <para>
    ///         Configures the property to use the H2 IDENTITY feature to generate values for new entities,
    ///         when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///         The column is written as <c>GENERATED ALWAYS AS IDENTITY</c>, and the application will not be able
    ///         to override this behavior by providing a value.
    ///     </para>
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseIdentityAlwaysColumn(this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        var property = propertyBuilder.Metadata;
        property.SetValueGenerationStrategy(H2ValueGenerationStrategy.IdentityAlwaysColumn);
        property.SetHiLoSequenceName(null);
        property.SetHiLoSequenceSchema(null);
        property.SetSequenceName(null);
        property.SetSequenceSchema(null);

        return propertyBuilder;
    }

    /// <summary>
    ///     <para>
    ///         Configures the property to use the H2 IDENTITY feature to generate values for new entities,
    ///         when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///         The column is written as <c>GENERATED ALWAYS AS IDENTITY</c>, and the application will not be able
    ///         to override this behavior by providing a value.
    ///     </para>
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseIdentityAlwaysColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder)
        => (PropertyBuilder<TProperty>)UseIdentityAlwaysColumn((PropertyBuilder)propertyBuilder);

    #endregion Identity always

    #region Identity by default

    /// <summary>
    ///     <para>
    ///         Configures the property to use the H2 IDENTITY feature to generate values for new entities,
    ///         when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///         The column is written as <c>GENERATED BY DEFAULT AS IDENTITY</c>, and the application will be able
    ///         to override this behavior by providing a value.
    ///     </para>
    ///     <para>
    ///         This is the default behavior when targeting H2.
    ///     </para>
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseIdentityByDefaultColumn(this PropertyBuilder propertyBuilder)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        var property = propertyBuilder.Metadata;
        property.SetValueGenerationStrategy(H2ValueGenerationStrategy.IdentityByDefaultColumn);
        property.SetHiLoSequenceName(null);
        property.SetHiLoSequenceSchema(null);
        property.SetSequenceName(null);
        property.SetSequenceSchema(null);

        return propertyBuilder;
    }

    /// <summary>
    ///     <para>
    ///         Configures the property to use the H2 IDENTITY feature to generate values for new entities,
    ///         when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///         The column is written as <c>GENERATED BY DEFAULT AS IDENTITY</c>, and the application will be able
    ///         to override this behavior by providing a value.
    ///     </para>
    ///     <para>
    ///         This is the default behavior when targeting H2.
    ///     </para>
    /// </summary>
    /// <typeparam name="TProperty"> The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseIdentityByDefaultColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder)
        => (PropertyBuilder<TProperty>)UseIdentityByDefaultColumn((PropertyBuilder)propertyBuilder);

    /// <summary>
    ///     <para>
    ///         Configures the property to use the H2 IDENTITY feature to generate values for new entities,
    ///         when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///         The column is written as <c>GENERATED BY DEFAULT AS IDENTITY</c>, and the application will be able
    ///         to override this behavior by providing a value.
    ///     </para>
    ///     <para>
    ///         This internally calls <see cref="UseIdentityByDefaultColumn(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)" />.
    ///         This is the default behavior when targeting H2.
    ///     </para>
    /// </summary>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder UseIdentityColumn(
        this PropertyBuilder propertyBuilder)
        => propertyBuilder.UseIdentityByDefaultColumn();

    /// <summary>
    ///     <para>
    ///         Configures the property to use the H2 IDENTITY feature to generate values for new entities,
    ///         when targeting H2. This method sets the property to be <see cref="ValueGenerated.OnAdd" />.
    ///         The column is written as <c>GENERATED BY DEFAULT AS IDENTITY</c>, and the application will be able
    ///         to override this behavior by providing a value.
    ///     </para>
    ///     <para>
    ///         This internally calls <see cref="UseIdentityByDefaultColumn(Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder)" />.
    ///         This is the default behavior when targeting H2.
    ///     </para>
    /// </summary>
    /// <typeparam name="TProperty"> The type of the property being configured.</typeparam>
    /// <param name="propertyBuilder"> The builder for the property being configured.</param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> UseIdentityColumn<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder)
        => propertyBuilder.UseIdentityByDefaultColumn();

    #endregion Identity by default

    #region General value generation strategy

    /// <summary>
    ///     Configures the value generation strategy for the key property, when targeting H2.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="valueGenerationStrategy">The value generation strategy.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns>
    ///     The same builder instance if the configuration was applied, <c>null</c> otherwise.
    /// </returns>
    public static IConventionPropertyBuilder? HasValueGenerationStrategy(
        this IConventionPropertyBuilder propertyBuilder,
        H2ValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
    {
        if (propertyBuilder.CanSetAnnotation(
                H2AnnotationNames.ValueGenerationStrategy, valueGenerationStrategy, fromDataAnnotation))
        {
            propertyBuilder.Metadata.SetValueGenerationStrategy(valueGenerationStrategy, fromDataAnnotation);

            if (valueGenerationStrategy != H2ValueGenerationStrategy.SequenceHiLo)
            {
                propertyBuilder.HasHiLoSequence(null, null, fromDataAnnotation);
            }

            if (valueGenerationStrategy != H2ValueGenerationStrategy.Sequence)
            {
                propertyBuilder.HasSequence(null, null, fromDataAnnotation);
            }

            return propertyBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the given value can be set as the value generation strategy for a particular table.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="valueGenerationStrategy">The value generation strategy.</param>
    /// <param name="storeObject">The table identifier.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><see langword="true" /> if the given value can be set as the default value generation strategy.</returns>
    public static bool CanSetValueGenerationStrategy(
        this IConventionPropertyBuilder propertyBuilder,
        H2ValueGenerationStrategy? valueGenerationStrategy,
        in StoreObjectIdentifier storeObject,
        bool fromDataAnnotation = false)
        => propertyBuilder.Metadata.FindOverrides(storeObject)?.Builder
                .CanSetAnnotation(
                    H2AnnotationNames.ValueGenerationStrategy,
                    valueGenerationStrategy,
                    fromDataAnnotation)
            ?? true;

    /// <summary>
    ///     Returns a value indicating whether the given value can be set as the value generation strategy.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="valueGenerationStrategy">The value generation strategy.</param>
    /// <param name="fromDataAnnotation">Indicates whether the configuration was specified using a data annotation.</param>
    /// <returns><c>true</c> if the given value can be set as the default value generation strategy.</returns>
    public static bool CanSetValueGenerationStrategy(
        this IConventionPropertyBuilder propertyBuilder,
        H2ValueGenerationStrategy? valueGenerationStrategy,
        bool fromDataAnnotation = false)
        => propertyBuilder.CanSetAnnotation(
            H2AnnotationNames.ValueGenerationStrategy, valueGenerationStrategy, fromDataAnnotation);

    #endregion General value generation strategy

    #region Identity options

    /// <summary>
    ///     Sets the sequence options on an identity column. The column must be set as identity via
    ///     <see cref="UseIdentityColumn(PropertyBuilder)" /> or <see cref="UseIdentityAlwaysColumn(PropertyBuilder)" />.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="startValue">
    ///     The starting value for the sequence.
    ///     The default starting value is <paramref name="minValue" /> for ascending sequences and <paramref name="maxValue" /> for
    ///     descending     ones.
    /// </param>
    /// <param name="incrementBy">The amount to increment between values. Defaults to 1.</param>
    /// <param name="minValue">
    ///     The minimum value for the sequence.
    ///     The default for an ascending sequence is 1. The default for a descending sequence is the minimum value of the data type.
    /// </param>
    /// <param name="maxValue">
    ///     The maximum value for the sequence.
    ///     The default for an ascending sequence is the maximum value of the data type. The default for a descending sequence is -1.
    /// </param>
    /// <param name="cyclic">
    ///     Sets whether or not the sequence will start again from the beginning once the maximum value is reached.
    ///     Defaults to false.
    /// </param>
    /// <param name="numbersToCache">
    ///     Specifies how many sequence numbers are to be pre0allocated and stored in memory for faster access.
    ///     The minimum value is 1 (only one value can be generated at a time, i.e., no cache), and this is also the default.
    /// </param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder HasIdentityOptions(
        this PropertyBuilder propertyBuilder,
        long? startValue = null,
        long? incrementBy = null,
        long? minValue = null,
        long? maxValue = null,
        bool? cyclic = null,
        long? numbersToCache = null)
    {
        var property = propertyBuilder.Metadata;
        property.SetIdentityStartValue(startValue);
        property.SetIdentityIncrementBy(incrementBy);
        property.SetIdentityMinValue(minValue);
        property.SetIdentityMaxValue(maxValue);
        property.SetIdentityIsCyclic(cyclic);
        property.SetIdentityNumbersToCache(numbersToCache);
        return propertyBuilder;
    }

    /// <summary>
    ///     Sets the sequence options on an identity column. The column must be set as identity via
    ///     <see cref="UseIdentityColumn(PropertyBuilder)" /> or <see cref="UseIdentityAlwaysColumn(PropertyBuilder)" />.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="startValue">
    ///     The starting value for the sequence.
    ///     The default starting value is <paramref name="minValue" /> for ascending sequences and <paramref name="maxValue" /> for descending
    ///     ones.
    /// </param>
    /// <param name="incrementBy">The amount to increment between values. Defaults to 1.</param>
    /// <param name="minValue">
    ///     The minimum value for the sequence.
    ///     The default for an ascending sequence is 1. The default for a descending sequence is the minimum value of the data type.
    /// </param>
    /// <param name="maxValue">
    ///     The maximum value for the sequence.
    ///     The default for an ascending sequence is the maximum value of the data type. The default for a descending sequence is -1.
    /// </param>
    /// <param name="cyclic">
    ///     Sets whether or not the sequence will start again from the beginning once the maximum value is reached.
    ///     Defaults to false.
    /// </param>
    /// <param name="numbersToCache">
    ///     Specifies how many sequence numbers are to be pre-allocated and stored in memory for faster access.
    ///     The minimum value is 1 (only one value can be generated at a time, i.e., no cache), and this is also the default.
    /// </param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static PropertyBuilder<TProperty> HasIdentityOptions<TProperty>(
        this PropertyBuilder<TProperty> propertyBuilder,
        long? startValue = null,
        long? incrementBy = null,
        long? minValue = null,
        long? maxValue = null,
        bool? cyclic = null,
        long? numbersToCache = null)
        => (PropertyBuilder<TProperty>)HasIdentityOptions(
            (PropertyBuilder)propertyBuilder, startValue, incrementBy, minValue, maxValue, cyclic, numbersToCache);

    /// <summary>
    ///     Sets the sequence options on an identity column. The column must be set as identity via
    ///     <see cref="UseIdentityColumn(PropertyBuilder)" /> or <see cref="UseIdentityAlwaysColumn(PropertyBuilder)" />.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="startValue">
    ///     The starting value for the sequence.
    ///     The default starting value is <paramref name="minValue" /> for ascending sequences and <paramref name="maxValue" /> for descending
    ///     ones.
    /// </param>
    /// <param name="incrementBy">The amount to increment between values. Defaults to 1.</param>
    /// <param name="minValue">
    ///     The minimum value for the sequence.
    ///     The default for an ascending sequence is 1. The default for a descending sequence is the minimum value of the data type.
    /// </param>
    /// <param name="maxValue">
    ///     The maximum value for the sequence.
    ///     The default for an ascending sequence is the maximum value of the data type. The default for a descending sequence is -1.
    /// </param>
    /// <param name="cyclic">
    ///     Sets whether or not the sequence will start again from the beginning once the maximum value is reached.
    ///     Defaults to false.
    /// </param>
    /// <param name="numbersToCache">
    ///     Specifies how many sequence numbers are to be pre-allocated and stored in memory for faster access.
    ///     The minimum value is 1 (only one value can be generated at a time, i.e., no cache), and this is also the default.
    /// </param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static IConventionPropertyBuilder? HasIdentityOptions(
        this IConventionPropertyBuilder propertyBuilder,
        long? startValue = null,
        long? incrementBy = null,
        long? minValue = null,
        long? maxValue = null,
        bool? cyclic = null,
        long? numbersToCache = null)
    {
        if (propertyBuilder.CanSetIdentityOptions(startValue, incrementBy, minValue, maxValue, cyclic, numbersToCache))
        {
            var property = propertyBuilder.Metadata;
            property.SetIdentityStartValue(startValue);
            property.SetIdentityIncrementBy(incrementBy);
            property.SetIdentityMinValue(minValue);
            property.SetIdentityMaxValue(maxValue);
            property.SetIdentityIsCyclic(cyclic);
            property.SetIdentityNumbersToCache(numbersToCache);
            return propertyBuilder;
        }

        return null;
    }

    /// <summary>
    ///     Returns a value indicating whether the sequence options can be set on the identity column.
    /// </summary>
    /// <param name="propertyBuilder">The builder for the property being configured.</param>
    /// <param name="startValue">
    ///     The starting value for the sequence. The default starting value is <paramref name="minValue" /> for ascending sequences and
    ///     <paramref name="maxValue" /> for descending ones.
    /// </param>
    /// <param name="incrementBy">The amount to increment between values. Defaults to 1.</param>
    /// <param name="minValue">
    ///     The minimum value for the sequence.
    ///     The default for an ascending sequence is 1. The default for a descending sequence is the minimum value of the data type.
    /// </param>
    /// <param name="maxValue">
    ///     The maximum value for the sequence.
    ///     The default for an ascending sequence is the maximum value of the data type. The default for a descending sequence is -1.
    /// </param>
    /// <param name="cyclic">
    ///     Sets whether or not the sequence will start again from the beginning once the maximum value is reached.
    ///     Defaults to false.
    /// </param>
    /// <param name="numbersToCache">
    ///     Specifies how many sequence numbers are to be pre-allocated and stored in memory for faster access.
    ///     The minimum value is 1 (only one value can be generated at a time, i.e., no cache), and this is also the default.
    /// </param>
    /// <returns>The same builder instance so that multiple calls can be chained.</returns>
    public static bool CanSetIdentityOptions(
        this IConventionPropertyBuilder propertyBuilder,
        long? startValue = null,
        long? incrementBy = null,
        long? minValue = null,
        long? maxValue = null,
        bool? cyclic = null,
        long? numbersToCache = null)
    {
        Check.NotNull(propertyBuilder, nameof(propertyBuilder));

        var value = new IdentitySequenceOptionsData
        {
            StartValue = startValue,
            IncrementBy = incrementBy ?? 1,
            MinValue = minValue,
            MaxValue = maxValue,
            IsCyclic = cyclic ?? false,
            NumbersToCache = numbersToCache ?? 1
        }.Serialize();

        return propertyBuilder.CanSetAnnotation(H2AnnotationNames.IdentityOptions, value);
    }

    #endregion Identity options



}
