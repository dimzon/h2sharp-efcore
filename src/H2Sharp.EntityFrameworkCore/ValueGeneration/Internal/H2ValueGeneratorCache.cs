using System.Collections.Concurrent;

namespace H2Sharp.EntityFrameworkCore.ValueGeneration.Internal;

/// <summary>
///     This API supports the Entity Framework Core infrastructure and is not intended to be used
///     directly from your code. This API may change or be removed in future releases.
/// </summary>
public class H2ValueGeneratorCache : ValueGeneratorCache, IH2ValueGeneratorCache
{
    private readonly ConcurrentDictionary<string, H2SequenceValueGeneratorState> _sequenceGeneratorCache
        = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueGeneratorCache" /> class.
    /// </summary>
    /// <param name="dependencies"> Parameter object containing dependencies for this service. </param>
    public H2ValueGeneratorCache(ValueGeneratorCacheDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This API supports the Entity Framework Core infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public virtual H2SequenceValueGeneratorState GetOrAddSequenceState(
        IProperty property,
        IRelationalConnection connection)
    {
        var sequence = property.FindHiLoSequence();

        Debug.Assert(sequence is not null);

        return _sequenceGeneratorCache.GetOrAdd(
            GetSequenceName(sequence, connection),
            _ => new H2SequenceValueGeneratorState(sequence));
    }

    private static string GetSequenceName(ISequence sequence, IRelationalConnection connection)
    {
        var dbConnection = connection.DbConnection;

        return dbConnection.Database.ToUpperInvariant()
            + "::"
            + dbConnection.DataSource.ToUpperInvariant()
            + "::"
            + (sequence.Schema is null ? "" : sequence.Schema + ".")
            + sequence.Name;
    }
}
