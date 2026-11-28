using System.IO;
using System.Text;
using System.Text.Json;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     Normalizes the JSON stream EF materializes from a JSON column. Tests seeded from the
///     PostgreSQL suite write plain string literals, which H2 stores as JSON string scalars;
///     EF's JSON reader expects the document the string contains.
/// </summary>
internal static class H2JsonStreamUnwrapper
{
    internal static MemoryStream Unwrap(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        byte[] bytes = buffer.ToArray();

        using (JsonDocument document = JsonDocument.Parse(bytes))
        {
            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                bytes = Encoding.UTF8.GetBytes(document.RootElement.GetString()!);
            }
        }

        return new MemoryStream(bytes, writable: false);
    }
}
