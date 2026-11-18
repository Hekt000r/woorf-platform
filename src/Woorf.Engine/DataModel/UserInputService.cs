using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed class UserInputService : Instance
{
    private readonly HashSet<(string Type, string Key)> keys = [];
    private readonly HashSet<(string Type, string Key)> consumed = [];
    public ScriptSignal InputBegan { get; } = new();
    public ScriptSignal InputEnded { get; } = new();
    public GuiEnum LastInputType { get; private set; } = GuiEnum.From("UserInputType", "Keyboard");
    public bool GamepadEnabled { get; private set; }
    internal bool HasInputSignal(string name) => name is "TouchStarted" or "TouchMoved" or "TouchEnded" or
        "GamepadConnected" or "GamepadDisconnected" or "LastInputTypeChanged" or "TextBoxFocused" or "TextBoxFocusReleased";

    protected override bool HasProperty(string property) => property is "GamepadEnabled" or "TouchEnabled"
        or "KeyboardEnabled" or "MouseEnabled" or "VREnabled" || base.HasProperty(property);
    internal UserInputService() : base("UserInputService") =>
        GetSignal("Destroying").Connect(_ => { InputBegan.Close(); InputEnded.Close(); });

    public bool IsKeyDown(GuiEnum key)
    {
        if (key.EnumType != "KeyCode") throw new InvalidOperationException("Expected Enum.KeyCode.");
        return keys.Contains(("Keyboard", key.Name));
    }

    public bool IsKeyConsumed(string key) => consumed.Contains(("Keyboard", key));

    public bool SetKey(string key, bool isDown, bool isProcessed) => SetInput("Keyboard", key, isDown, isProcessed);

    public bool SetInput(string inputType, string key, bool isDown, bool isProcessed)
    {
        var identity = (inputType, key);
        if (isDown == keys.Contains(identity)) return consumed.Contains(identity);
        var type = GuiEnum.From("UserInputType", inputType);
        GuiEnum.From("KeyCode", key);
        if (!GamepadEnabled && inputType == "Gamepad1")
        {
            GamepadEnabled = true;
            FirePropertyChanged(nameof(GamepadEnabled));
            GetSignal("GamepadConnected").Fire(type);
        }
        if (isDown && LastInputType != type)
        {
            LastInputType = type;
            GetSignal("LastInputTypeChanged").Fire(type);
        }
        if (isDown) keys.Add(identity);
        else keys.Remove(identity);
        var input = new GuiInputObject(default, default, inputType, isDown ? "Begin" : "End", key);
        if (!isProcessed && Parent is DataModel game &&
            game.FindService("ContextActionService") is ContextActionService actions)
            isProcessed = actions.Dispatch(input);
        if (isDown && isProcessed) consumed.Add(identity);
        else if (!isDown) isProcessed |= consumed.Remove(identity);
        (isDown ? InputBegan : InputEnded).Fire(input, isProcessed);
        return isProcessed;
    }

    public void ReleaseKeys()
    {
        foreach (var (type, key) in keys.ToArray()) SetInput(type, key, false, false);
    }
}
