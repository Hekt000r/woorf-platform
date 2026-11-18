using Woorf.Engine.DataModel;

namespace Woorf.Engine.Networking;

internal sealed record ReplicatedBackendState(IReadOnlyDictionary<string, object> Attributes, string[] Tags,
    object? Value, double Health, double MaxHealth, Instance? Team, Instance? ResolvedValue = null, Instance? ResolvedTeam = null, ToolState? Tool = null, object? PartColor = null, int? TeamColorNumber = null)
{
    internal static ReplicatedBackendState Capture(Instance instance) => new(instance.GetAttributes(), instance.GetTags(),
        instance is ValueObject value ? value.Value : null,
        instance is Humanoid humanoid ? humanoid.Health : 0,
        instance is Humanoid sized ? sized.MaxHealth : 0,
        instance is Player player ? player.Team : null, Tool: ToolState.Capture(instance),
        PartColor: (instance as BasePart)?.ColorOverride,
        TeamColorNumber: instance switch { Team team => team.TeamColor.Number, Player owner => owner.NeutralTeamColor.Number, _ => null });

    internal void Apply(Instance instance, ReplicatedBackendState? previous, Func<Instance, Instance?> translate)
    {
        foreach (var name in (previous?.Attributes.Keys ?? []).Concat(Attributes.Keys).Distinct())
        {
            var next = Attributes.GetValueOrDefault(name);
            var old = previous?.Attributes.GetValueOrDefault(name);
            if (previous is null || !Equals(next, old)) instance.SetAttribute(name, next);
        }
        foreach (var tag in (previous?.Tags ?? []).Except(Tags)) instance.RemoveTag(tag);
        foreach (var tag in Tags.Except(previous?.Tags ?? [])) instance.AddTag(tag);
        var mapped = Value is Instance reference ? translate(reference) : Value;
        if (instance is ValueObject value && (previous is null || !Equals(Value, previous.Value)
            || Value is Instance && !ReferenceEquals(mapped, previous.ResolvedValue)))
            value.Value = mapped;
        if (instance is Humanoid humanoid)
        {
            if (previous is null || MaxHealth != previous.MaxHealth) humanoid.MaxHealth = MaxHealth;
            if (previous is null || Health != previous.Health) humanoid.Health = Health;
        }
        if (instance is Tool tool && Tool is { } state) state.Apply(tool, previous?.Tool);
        if (instance is BasePart part && (previous is null || !Equals(PartColor, previous.PartColor)))
            part.SetColorOverride(PartColor);
        if (TeamColorNumber is int number && number != previous?.TeamColorNumber)
        {
            if (instance is Team team) team.TeamColor = new(number);
            if (instance is Player owner) owner.SetNeutralTeamColor(new(number));
        }
        var mappedTeam = Team is null ? null : translate(Team);
        if (instance is Player player && (previous is null || Team != previous.Team || mappedTeam != previous.ResolvedTeam))
            player.Team = mappedTeam;
    }

    internal ReplicatedBackendState ResolveReferences(Func<Instance, Instance?> translate) => this with
    {
        ResolvedValue = Value is Instance reference ? translate(reference) : null,
        ResolvedTeam = Team is null ? null : translate(Team),
    };

    internal WireBackendState Encode(Func<Instance, int> identify) => new(
        Attributes.ToDictionary(entry => entry.Key, entry => WireValue.Encode(entry.Value, identify)),
        Tags, WireValue.Encode(Value, identify), Health, MaxHealth, Team is null ? 0 : identify(Team), Tool?.Encode(),
        PartColor is null ? null : WireValue.Encode(PartColor, identify), TeamColorNumber);
}

internal sealed record WireBackendState(Dictionary<string, WireValue> Attributes, string[] Tags,
    WireValue Value, double Health, double MaxHealth, int Team, WireToolState? Tool = null, WireValue? PartColor = null, int? TeamColorNumber = null)
{
    internal ReplicatedBackendState Decode(Func<int, Instance?> find) => new(
        Attributes.ToDictionary(entry => entry.Key, entry => entry.Value.Decode(find)!),
        Tags, Value.Decode(find), Health, MaxHealth, find(Team), Tool: Tool?.Decode(), PartColor: PartColor?.Decode(find), TeamColorNumber: TeamColorNumber);
}
