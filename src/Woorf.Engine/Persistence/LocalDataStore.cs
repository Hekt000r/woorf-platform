using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Woorf.Engine.Persistence;

internal sealed class LocalDataStore(string directory, string name, string scope)
{
    private string PathName => Path.Combine(directory, Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new[] { name, scope }))) + ".json");
    internal JsonElement? Read(string key) => Execute(values => values.TryGetValue(key, out var value) ? value.Clone() : (JsonElement?)null, false);
    internal void Set(string key, JsonElement value) => Execute(values => { values[key] = value.Clone(); return 0; }, true);
    internal JsonElement? Remove(string key) => Execute(values => values.Remove(key, out var previous) ? previous.Clone() : (JsonElement?)null, true);
    internal double Increment(string key, double delta) => Execute(values =>
    {
        var previous = values.TryGetValue(key, out var value) ? value.GetDouble() : 0;
        var next = previous + delta;
        if (!double.IsFinite(next)) throw new InvalidOperationException("Invalid increment result.");
        values[key] = JsonSerializer.SerializeToElement(next);
        return next;
    }, true);

    internal JsonElement? Update(string key, Func<JsonElement?, JsonElement?> transform) => Execute(values =>
    {
        var previous = values.TryGetValue(key, out var stored) ? stored.Clone() : (JsonElement?)null;
        var next = transform(previous);
        if (next is JsonElement value) values[key] = value.Clone();
        return next;
    }, true);

    private T Execute<T>(Func<Dictionary<string, JsonElement>, T> action, bool shouldWrite)
    {
        Directory.CreateDirectory(directory);
        using var gate = Acquire();
        var values = File.Exists(PathName)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText(PathName)) ?? []
            : new Dictionary<string, JsonElement>();
        var result = action(values);
        if (!shouldWrite) return result;
        var temporary = PathName + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, values);
                stream.Flush(true);
            }
            File.Move(temporary, PathName, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return result;
    }

    private FileStream Acquire()
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            try { return new FileStream(PathName + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (started.Elapsed.TotalSeconds < 5) { Thread.Sleep(5); }
        }
    }
}
