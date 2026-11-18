namespace Woorf.Engine.Datatypes;

public sealed record GuiInputObject(Vector3 Position, Vector3 Delta,
    string InputType = "MouseMovement", string InputState = "Change", string KeyCode = "Unknown");
