using System.Text.Json;
using System.Text.Json.Nodes;

namespace Woorf.Engine.Networking;

internal sealed record WirePatch(int Id, Dictionary<string, JsonElement> Properties);

internal sealed class ReplicationBaseline
{
    private Dictionary<int, JsonElement> previous = [];

    internal void Initialize(WireNode[] nodes) => previous = Capture(nodes);

    internal WirePacket? BuildDelta(WireNode[] nodes, int[] removedIds)
    {
        var current = Capture(nodes);
        var added = nodes.Where(node => !previous.ContainsKey(node.Id)).ToArray();
        var updated = new List<WirePatch>();
        foreach (var node in nodes)
        {
            if (!previous.TryGetValue(node.Id, out var old)) continue;
            var properties = new Dictionary<string, JsonElement>();
            foreach (var property in current[node.Id].EnumerateObject())
            {
                if (property.Name is "Id" or "ClassName") continue;
                if (property.Value.GetRawText() != old.GetProperty(property.Name).GetRawText())
                    properties.Add(property.Name, property.Value);
            }
            if (properties.Count > 0) updated.Add(new(node.Id, properties));
        }
        var removed = removedIds.Where(previous.ContainsKey).ToArray();
        foreach (var id in removed) previous.Remove(id);
        foreach (var entry in current) previous[entry.Key] = entry.Value;
        return added.Length == 0 && updated.Count == 0 && removed.Length == 0 ? null
            : new("delta", Nodes: added, Updates: updated.ToArray(), Removed: removed);
    }

    private static Dictionary<int, JsonElement> Capture(WireNode[] nodes) => nodes.ToDictionary(
        node => node.Id, node => JsonSerializer.SerializeToElement(node, SocketPeer.Options));
}

internal sealed class ReplicaUpdates(WireGraph graph)
{
    private readonly Dictionary<int, WireNode> nodes = [];

    internal void Initialize(WireNode[] initial)
    {
        nodes.Clear();
        foreach (var node in initial) nodes.Add(node.Id, node);
        graph.Apply(initial);
    }

    internal void Apply(WirePacket packet)
    {
        if (packet.Nodes is null || packet.Updates is null || packet.Removed is null)
            throw new InvalidDataException("Incomplete replication delta.");
        foreach (var node in packet.Nodes) nodes.Add(node.Id, node);
        foreach (var patch in packet.Updates)
        {
            if (!nodes.TryGetValue(patch.Id, out var original))
                throw new InvalidDataException("Unknown replication update target.");
            var merged = JsonSerializer.SerializeToNode(original, SocketPeer.Options)!.AsObject();
            foreach (var property in patch.Properties)
            {
                if (property.Key is "Id" or "ClassName" || !merged.ContainsKey(property.Key))
                    throw new InvalidDataException("Invalid replicated property.");
                merged[property.Key] = JsonNode.Parse(property.Value.GetRawText());
            }
            nodes[patch.Id] = merged.Deserialize<WireNode>(SocketPeer.Options)
                ?? throw new InvalidDataException("Invalid replication update.");
        }
        foreach (var id in packet.Removed) nodes.Remove(id);
        var changed = packet.Nodes.Select(node => node.Id).Concat(packet.Updates.Select(patch => patch.Id)).Distinct();
        graph.ApplyChanges(changed.Where(nodes.ContainsKey).Select(id => nodes[id]).ToArray(), packet.Removed);
    }
}
