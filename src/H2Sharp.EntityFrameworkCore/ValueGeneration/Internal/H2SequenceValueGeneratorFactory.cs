using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.ValueGeneration.Internal;

/// <summary>
///     This API supports the Entity Framework Core infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
public class H2SequenceValueGeneratorFactory : IH2SequenceValueGeneratorFactory
{
    private readonly IUpdateSqlGenerator _sqlGenerator;

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public H2SequenceValueGeneratorFactory(
        IUpdateSqlGenerator sqlGenerator)
    {
        Check.NotNull(sqlGenerator, nameof(sqlGenerator));

        _sqlGenerator = sqlGenerator;
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual ValueGenerator? TryCreate(
        IProperty property,
        Type type,
        H2SequenceValueGeneratorState generatorState,
        IH2RelationalConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalCommandDiagnosticsLogger commandLogger)
    {
        if (type == typeof(long))
        {
            return new H2SequenceHiLoValueGenerator<long>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(int))
        {
            return new H2SequenceHiLoValueGenerator<int>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(short))
        {
            return new H2SequenceHiLoValueGenerator<short>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(byte))
        {
            return new H2SequenceHiLoValueGenerator<byte>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(char))
        {
            return new H2SequenceHiLoValueGenerator<char>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(ulong))
        {
            return new H2SequenceHiLoValueGenerator<ulong>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(uint))
        {
            return new H2SequenceHiLoValueGenerator<uint>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(ushort))
        {
            return new H2SequenceHiLoValueGenerator<ushort>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        if (type == typeof(sbyte))
        {
            return new H2SequenceHiLoValueGenerator<sbyte>(
                rawSqlCommandBuilder, _sqlGenerator, generatorState, connection, commandLogger);
        }

        return null;
    }
}
