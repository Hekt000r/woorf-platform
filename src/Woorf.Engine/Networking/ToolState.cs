using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed record ToolState(bool Enabled, bool RequiresHandle, bool CanBeDropped, bool ManualActivationOnly,
    string ToolTip, string TextureId, CFrame Grip)
{
    internal static ToolState? Capture(Instance instance) => instance is Tool tool
        ? new(tool.Enabled, tool.RequiresHandle, tool.CanBeDropped, tool.ManualActivationOnly, tool.ToolTip, tool.TextureId, tool.Grip)
        : null;

    internal void Apply(Tool tool, ToolState? previous)
    {
        if (Enabled != previous?.Enabled) tool.Enabled = Enabled;
        if (RequiresHandle != previous?.RequiresHandle) tool.RequiresHandle = RequiresHandle;
        if (CanBeDropped != previous?.CanBeDropped) tool.CanBeDropped = CanBeDropped;
        if (ManualActivationOnly != previous?.ManualActivationOnly) tool.ManualActivationOnly = ManualActivationOnly;
        if (ToolTip != previous?.ToolTip) tool.ToolTip = ToolTip;
        if (TextureId != previous?.TextureId) tool.TextureId = TextureId;
        if (Grip != previous?.Grip) tool.Grip = Grip;
    }

    internal WireToolState Encode() => new(Enabled, RequiresHandle, CanBeDropped, ManualActivationOnly, ToolTip, TextureId, Grip.GetComponents());
}

internal sealed record WireToolState(bool Enabled, bool RequiresHandle, bool CanBeDropped, bool ManualActivationOnly,
    string ToolTip, string TextureId, float[] Grip)
{
    internal ToolState Decode() => new(Enabled, RequiresHandle, CanBeDropped, ManualActivationOnly, ToolTip, TextureId, CFrame.FromComponents(Grip));
}
