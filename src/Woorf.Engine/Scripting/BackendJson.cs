using System.Text.Json;

namespace Woorf.Engine.Scripting;

internal static class BackendJson
{
    internal static object? ToJsonValue(object? value, int depth = 0)
    {
        if (depth > 64) throw new InvalidOperationException("JSON nesting too deep.");
        if (value is null or string or bool) return value;
        if (value is double number && double.IsFinite(number)) return number;
        if (value is not LuaTableValue table) throw new InvalidOperationException("Unsupported JSON value.");
        var numeric = table.Entries.All(entry => entry.Key is double n && n >= 1 && n == Math.Truncate(n));
        if (numeric && table.Entries.Count > 0)
        {
            var ordered = table.Entries.OrderBy(entry => (double)entry.Key).ToArray();
            if (ordered.Where((entry, index) => (double)entry.Key != index + 1).Any()) throw new InvalidOperationException("JSON arrays must be contiguous.");
            return ordered.Select(entry => ToJsonValue(entry.Value, depth + 1)).ToArray();
        }
        var dictionary = new Dictionary<string, object?>();
        foreach (var entry in table.Entries)
        {
            if (entry.Key is not string key) throw new InvalidOperationException("JSON object keys must be strings.");
            dictionary.Add(key, ToJsonValue(entry.Value, depth + 1));
        }
        return dictionary;
    }

    internal static object? FromJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null, JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.String => value.GetString(), JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.Array => new LuaTableValue(value.EnumerateArray().Select((item, index) =>
            new KeyValuePair<object, object?>((double)index + 1, FromJson(item))).ToArray()),
        JsonValueKind.Object => new LuaTableValue(value.EnumerateObject().Select(property =>
            new KeyValuePair<object, object?>(property.Name, FromJson(property.Value))).ToArray()),
        _ => throw new InvalidOperationException("Unsupported JSON value."),
    };
}

internal sealed partial class BackendBindings
{
    private int CallJson(nint state, string method)
    {
        if (method == "JSONEncode") return Push(state, JsonSerializer.Serialize(BackendJson.ToJsonValue(Read(state, 2))));
        using var document = JsonDocument.Parse(Text(state, 2));
        return Push(state, BackendJson.FromJson(document.RootElement));
    }
}
