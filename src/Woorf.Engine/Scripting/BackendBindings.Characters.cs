using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private Action<Player>? loadCharacter;
    internal void AttachCharacters(Action<Player> load) => loadCharacter = load;

    private bool TryReadCharacter(nint state, Instance instance, string property)
    {
        if (instance is Player player)
        {
            switch (property)
            {
                case "CameraMinZoomDistance": Push(state, player.CameraMinZoomDistance); return true;
                case "CameraMaxZoomDistance": Push(state, player.CameraMaxZoomDistance); return true;
                case "CameraMode": Push(state, player.CameraMode); return true;
            }
            if (property == "Character") { Push(state, player.Character); return true; }
            if (property is "CharacterAdded" or "CharacterRemoving")
            {
                signals.PushSignal(state, property == "CharacterAdded" ? player.CharacterAdded : player.CharacterRemoving);
                return true;
            }
        }
        if (instance is Players players && property is "CharacterAutoLoads" or "RespawnTime")
        {
            Push(state, property == "CharacterAutoLoads" ? (object)players.CharacterAutoLoads : players.RespawnTime); return true;
        }
        if (instance is not Humanoid humanoid) return false;
        object? value = property switch
        {
            "WalkSpeed" => humanoid.WalkSpeed, "JumpPower" => humanoid.JumpPower,
            "AutoRotate" => humanoid.AutoRotate, "MoveDirection" => humanoid.MoveDirection, "Jump" => humanoid.Jump, _ => null,
        };
        if (value is null) return false;
        Push(state, value); return true;
    }

    private bool TryWriteCharacter(nint state, Instance instance, string property)
    {
        if (instance is Player player)
        {
            switch (property)
            {
                case "CameraMinZoomDistance": player.CameraMinZoomDistance = Number(state, 3); return true;
                case "CameraMaxZoomDistance": player.CameraMaxZoomDistance = Number(state, 3); return true;
                case "CameraMode":
                    player.CameraMode = Read(state, 3) as GuiEnum ?? throw new ArgumentException("Expected Enum.CameraMode.");
                    return true;
            }
        }
        if (instance is Players players)
        {
            if (property == "CharacterAutoLoads") { players.CharacterAutoLoads = Read(state, 3) is bool value ? value : throw new ArgumentException("Expected boolean."); return true; }
            if (property == "RespawnTime") { players.RespawnTime = Number(state, 3); return true; }
        }
        if (instance is not Humanoid humanoid) return false;
        switch (property)
        {
            case "AutoRotate": humanoid.AutoRotate = Read(state, 3) is bool rotate ? rotate : throw new ArgumentException("Expected boolean."); break;
            case "WalkSpeed": humanoid.WalkSpeed = Number(state, 3); break;
            case "JumpPower": humanoid.JumpPower = Number(state, 3); break;
            case "Jump": humanoid.Jump = Read(state, 3) is bool jump ? jump : throw new ArgumentException("Expected boolean."); break;
            default: return false;
        }
        return true;
    }

    private void CallCharacter(nint state, Instance instance, string method)
    {
        if (method == "LoadCharacter")
        {
            (loadCharacter ?? throw new InvalidOperationException("LoadCharacter requires an authoritative runtime."))((Player)instance);
            return;
        }
        if (Read(state, 3) is not (null or false)) throw new ArgumentException("Only world-relative movement is supported.");
        ((Humanoid)instance).Move(Read(state, 2) is Vector3 direction ? direction : throw new ArgumentException("Expected Vector3 direction."));
    }
}
