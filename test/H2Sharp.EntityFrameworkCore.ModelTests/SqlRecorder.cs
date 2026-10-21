using System.Collections.Generic;
using System.Data.Common;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     Records the statements the provider sends, so that a test can say what the provider
///     wrote without waiting for the engine to run it.
/// </summary>
public sealed class SqlRecorder : DbCommandInterceptor
{
    public List<string> Commands { get; } = [];

    public override DbCommand CommandInitialized(CommandEndEventData eventData, DbCommand result)
    {
        Commands.Add(result.CommandText);
        return result;
    }
}
