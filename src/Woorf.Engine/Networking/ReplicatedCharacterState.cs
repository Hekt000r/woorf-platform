using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed record ReplicatedCharacterState(Model? Character, double WalkSpeed, double JumpPower,
    Vector3 MoveDirection, bool Jump, bool CharacterAutoLoads, double RespawnTime)
{
    internal static ReplicatedCharacterState? Capture(Instance instance) => instance switch
    {
        Player player => new(player.Character, 0, 0, default, false, false, 0),
        Humanoid humanoid => new(null, humanoid.WalkSpeed, humanoid.JumpPower, humanoid.MoveDirection, humanoid.Jump, false, 0),
        Players players => new(null, 0, 0, default, false, players.CharacterAutoLoads, players.RespawnTime),
        _ => null,
    };

    internal void ApplyValues(Instance instance, ReplicatedCharacterState? previous)
    {
        if (instance is Humanoid humanoid)
        {
            if (previous is null || previous.WalkSpeed != WalkSpeed) humanoid.WalkSpeed = WalkSpeed;
            if (previous is null || previous.JumpPower != JumpPower) humanoid.JumpPower = JumpPower;
            if (previous is null || previous.MoveDirection != MoveDirection) humanoid.Move(MoveDirection);
            if (previous is null || previous.Jump != Jump) humanoid.Jump = Jump;
        }
        if (instance is Players players)
        {
            if (previous is null || previous.CharacterAutoLoads != CharacterAutoLoads) players.CharacterAutoLoads = CharacterAutoLoads;
            if (previous is null || previous.RespawnTime != RespawnTime) players.RespawnTime = RespawnTime;
        }
    }

    internal void ApplyReference(Player player, Func<Instance, Instance?> translate) =>
        player.Character = Character is null ? null : translate(Character) as Model;

    internal WireCharacterState Encode(Func<Instance, int> identify) => new(Character is null ? 0 : identify(Character),
        WalkSpeed, JumpPower, [MoveDirection.X, MoveDirection.Y, MoveDirection.Z], Jump, CharacterAutoLoads, RespawnTime);
}

internal sealed record WireCharacterState(int Character, double WalkSpeed, double JumpPower,
    float[] MoveDirection, bool Jump, bool CharacterAutoLoads, double RespawnTime)
{
    internal ReplicatedCharacterState Decode(Func<int, Instance?> find)
    {
        if (MoveDirection.Length != 3) throw new InvalidDataException("Invalid character movement direction.");
        return new(find(Character) as Model, WalkSpeed, JumpPower, new(MoveDirection[0], MoveDirection[1], MoveDirection[2]), Jump, CharacterAutoLoads, RespawnTime);
    }
}
