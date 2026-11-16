using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Woorf.Engine.Persistence;

namespace Woorf.Engine.Networking;

internal sealed class ServerIdentities(string directory)
{
    private readonly LocalDataStore store = new(directory, "ServerIdentities", "private");
    internal (int UserId, string Token) Resolve(string? credential)
    {
        if (credential is not null && (credential.Length != 64 || !credential.All(Uri.IsHexDigit)))
            throw new InvalidDataException("Invalid identity credential.");
        var token = credential ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var userId = 0;
        store.Update("registry", old =>
        {
            var entries = old is JsonElement value ? value.Deserialize<Dictionary<string, int>>()! : [];
            if (entries.TryGetValue(hash, out userId)) return old;
            if (credential is not null) throw new InvalidDataException("Unknown identity credential.");
            userId = checked(entries.Values.DefaultIfEmpty(0).Max() + 1);
            entries.Add(hash, userId);
            return JsonSerializer.SerializeToElement(entries);
        });
        return (userId, token);
    }
}
