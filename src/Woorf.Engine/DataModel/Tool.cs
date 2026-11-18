using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed class Tool : Model
{
    private bool isActive;
    private Model? equippedCharacter;
    private bool enabled = true, requiresHandle = true, canBeDropped = true, manualActivationOnly;
    private string toolTip = "", textureId = "";
    private CFrame grip = CFrame.Identity;

    internal Tool() : base("Tool")
    {
        GetPropertyChangedSignal("Parent").Connect(_ => UpdateEquipment());
        GetSignal("ChildAdded").Connect(_ => UpdateEquipment());
        GetSignal("ChildRemoved").Connect(_ => UpdateEquipment());
        GetSignal("Destroying").Connect(_ =>
        {
            Deactivate();
            Activated.Close(); Deactivated.Close(); Equipped.Close(); Unequipped.Close();
        });
    }

    public bool Enabled { get => enabled; set { Set(ref enabled, value, nameof(Enabled)); if (!value) Deactivate(); } }
    public bool RequiresHandle { get => requiresHandle; set { Set(ref requiresHandle, value, nameof(RequiresHandle)); UpdateEquipment(); } }
    public bool CanBeDropped { get => canBeDropped; set => Set(ref canBeDropped, value, nameof(CanBeDropped)); }
    public bool ManualActivationOnly { get => manualActivationOnly; set => Set(ref manualActivationOnly, value, nameof(ManualActivationOnly)); }
    public string ToolTip { get => toolTip; set => Set(ref toolTip, value, nameof(ToolTip)); }
    public string TextureId { get => textureId; set => Set(ref textureId, value, nameof(TextureId)); }
    public CFrame Grip { get => grip; set => Set(ref grip, value, nameof(Grip)); }
    public ScriptSignal Activated { get; } = new();
    public ScriptSignal Deactivated { get; } = new();
    public ScriptSignal Equipped { get; } = new();
    public ScriptSignal Unequipped { get; } = new();
    internal BasePart? Handle => FindFirstChild("Handle") as BasePart;

    internal bool IsHeld => Parent is Model character && character.FindFirstChild("Humanoid") is Humanoid;

    internal void UpdateHeldParts(Part root)
    {
        if (Handle is not { } handle) return;
        var hand = root.CFrame * new CFrame(new Vector3(.85f, -.35f, -.55f));
        var target = hand * Grip.Inverse();
        if ((handle.Position - target.Position).Magnitude < .00001f && handle.CFrame.Rotation == target.Rotation) return;
        var delta = target * handle.CFrame.Inverse();
        foreach (var part in GetDescendants().OfType<BasePart>())
            part.CFrame = delta * part.CFrame;
    }

    public void Activate()
    {
        if (isActive || !Enabled || (RequiresHandle && Handle is null) ||
            Parent?.FindFirstChild("Humanoid") is not Humanoid { Health: > 0 }) return;
        isActive = true;
        Activated.Fire();
    }

    public void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        Deactivated.Fire();
    }

    private void UpdateEquipment()
    {
        var next = (!RequiresHandle || Handle is not null) && Parent is Model model &&
            model.FindFirstChild("Humanoid") is Humanoid ? model : null;
        if (equippedCharacter == next) return;
        var hadEquipment = equippedCharacter is not null;
        equippedCharacter = next;
        Deactivate();
        if (hadEquipment) Unequipped.Fire();
        if (next is not null) Equipped.Fire();
    }

    private void Set<T>(ref T field, T value, string property)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        FirePropertyChanged(property);
    }

    protected override bool HasProperty(string property) => property is "Enabled" or "RequiresHandle" or "CanBeDropped"
        or "ManualActivationOnly" or "ToolTip" or "TextureId" or "Grip" || base.HasProperty(property);

    protected override void CopyPropertiesTo(Instance clone)
    {
        base.CopyPropertiesTo(clone);
        var tool = (Tool)clone;
        tool.enabled = enabled; tool.requiresHandle = requiresHandle; tool.canBeDropped = canBeDropped;
        tool.manualActivationOnly = manualActivationOnly; tool.toolTip = toolTip; tool.textureId = textureId; tool.grip = grip;
    }
}
