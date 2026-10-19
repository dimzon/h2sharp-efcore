using H2Sharp.EntityFrameworkCore.Storage.Internal;

namespace H2Sharp.EntityFrameworkCore.ValueGeneration.Internal;

/// <summary>
///     This API supports the Entity Framework Core infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
public interface IH2SequenceValueGeneratorFactory
{
    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    ValueGenerator? TryCreate(
        IProperty property,
        Type clrType,
        H2SequenceValueGeneratorState generatorState,
        IH2RelationalConnection connection,
        IRawSqlCommandBuilder rawSqlCommandBuilder,
        IRelationalCommandDiagnosticsLogger commandLogger);
}
