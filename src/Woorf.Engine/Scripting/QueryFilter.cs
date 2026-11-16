using Woorf.Engine.DataModel;

namespace Woorf.Engine.Scripting;

internal sealed class QueryFilter
{
    private readonly Instance[] instances = [];
    private readonly bool include, respectCanCollide;
    internal QueryFilter(object? options)
    {
        if (options is null) return;
        if (options is not LuaTableValue table) throw new ArgumentException("Expected query options.");
        var values = table.Entries.ToDictionary(entry => (string)entry.Key, entry => entry.Value);
        if (values.GetValueOrDefault("Instances") is not LuaTableValue list
            || values.GetValueOrDefault("Include") is not bool shouldInclude
            || values.GetValueOrDefault("RespectCanCollide") is not bool shouldRespect)
            throw new ArgumentException("Invalid query options.");
        instances = list.Entries.Select(entry => entry.Value as Instance ?? throw new ArgumentException("Expected Instance filter.")).ToArray();
        include = shouldInclude; respectCanCollide = shouldRespect;
    }
    internal bool Allows(BasePart part)
    {
        if (part.IsDestroyed || respectCanCollide && !part.CanCollide) return false;
        if (!float.IsFinite(part.Size.Magnitude) || !float.IsFinite(part.Position.Magnitude)
            || !float.IsFinite(part.CFrame.RightVector.Magnitude) || !float.IsFinite(part.CFrame.UpVector.Magnitude)
            || !float.IsFinite(part.CFrame.ZVector.Magnitude)) return false;
        var matches = instances.Any(instance => part == instance || part.IsDescendantOf(instance));
        return include ? matches : !matches;
    }
}
