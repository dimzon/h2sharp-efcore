using System.Text.Json;
using H2Sharp.Data;
using H2Sharp.Data.Extensions;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     Provider-owned JSON path functions. H2 2.5 has only JSON_OBJECT/JSON_ARRAY, so EF's
///     JsonScalarExpression paths are translated to these registered .NET functions (ADR-001)
///     instead of PostgreSQL's -&gt; operators.
/// </summary>
internal static class H2JsonPathUdfs
{
    internal const string Extract = "____ef_json_extract";
    internal const string Query = "____ef_json_query";
    internal const string ArrayElementsText = "____ef_json_array_elements_text";

    internal static void Register(IH2Extensions extensions)
    {
        extensions.RegisterUdf<string?, string?, string?>(
            Extract, (document, segment) => JsonPath.ExtractText(document, segment), deterministic: true);
        extensions.RegisterUdf<string?, string?, string?>(
            Query, (document, segment) => JsonPath.QueryJson(document, segment), deterministic: true);
        extensions.RegisterTableUdf<string?>(ArrayElementsText, JsonPath.ArrayElementsText, deterministic: true);
    }

    private static class JsonPath
    {
        internal static string? ExtractText(string? document, string? segment)
        {
            if (document is null || segment is null)
            {
                return null;
            }

            using JsonDocument json = ParseDocument(document);
            JsonElement value = Select(json.RootElement, segment);
            return value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.GetRawText();
        }

        internal static string? QueryJson(string? document, string? segment)
        {
            if (document is null || segment is null)
            {
                return null;
            }

            using JsonDocument json = ParseDocument(document);
            return Select(json.RootElement, segment).GetRawText();
        }

        internal static H2TableUdfResult ArrayElementsText(string? document)
        {
            H2TableUdfColumn[] columns =
            [
                new("value", typeof(string)),
                new("ordinality", typeof(int))
            ];
            if (document is null)
            {
                return new H2TableUdfResult(columns, Array.Empty<object?[]>());
            }

            using JsonDocument json = ParseDocument(document);
            object?[][] rows = json.RootElement.EnumerateArray()
                .Select((element, index) => new object?[]
                {
                    element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText(),
                    index + 1
                })
                .ToArray();
            return new H2TableUdfResult(columns, rows);
        }

        // The PG test suites seed JSON with plain string literals, which H2 stores as JSON
        // string scalars. The provider's own writes are documents; unwrap the scalar form so
        // both shapes answer the same EF path.
        private static JsonDocument ParseDocument(string text)
        {
            JsonDocument document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind != JsonValueKind.String)
            {
                return document;
            }

            string inner = document.RootElement.GetString()!;
            document.Dispose();
            return JsonDocument.Parse(inner);
        }

        private static JsonElement Select(JsonElement element, string segment)
            => segment.Length >= 2 && segment[0] == '[' && segment[^1] == ']'
                ? element[int.Parse(segment[1..^1])]
                : element.GetProperty(segment);
    }
}
