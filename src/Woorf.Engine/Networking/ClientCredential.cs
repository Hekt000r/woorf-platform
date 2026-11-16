using System.Text.Json;

namespace Woorf.Engine.Networking;

internal sealed record ClientCredential(string Token)
{
    internal static string? Load(string? path)
    {
        if (path is null || !File.Exists(path)) return null;
        var value = JsonSerializer.Deserialize<ClientCredential>(File.ReadAllText(path));
        if (value?.Token is not { Length: 64 } token || !token.All(Uri.IsHexDigit))
            throw new InvalidDataException("Invalid client identity file.");
        return token;
    }

    internal static void Save(string? path, string? token)
    {
        if (path is null) return;
        if (token is not { Length: 64 } || !token.All(Uri.IsHexDigit)) throw new InvalidDataException("Missing server identity credential.");
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, new ClientCredential(token));
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
