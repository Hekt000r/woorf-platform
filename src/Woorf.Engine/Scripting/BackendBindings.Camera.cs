using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadCamera(nint state, Instance instance, string property)
    {
        if (instance is Workspace workspace && property == "CurrentCamera")
        {
            Push(state, workspace.CurrentCamera);
            return true;
        }
        if (instance is not Camera camera) return false;
        switch (property)
        {
            case "CFrame": Push(state, camera.CFrame); break;
            case "Focus": Push(state, camera.Focus); break;
            case "FieldOfView": Push(state, camera.FieldOfView); break;
            case "ViewportSize": Push(state, camera.ViewportSize); break;
            case "CameraType": Push(state, camera.CameraType); break;
            case "CameraSubject": Push(state, camera.CameraSubject); break;
            default: return false;
        }
        return true;
    }

    private bool TryWriteCamera(nint state, Instance instance, string property)
    {
        if (instance is Workspace workspace && property == "CurrentCamera")
        {
            if (!((RunService)game.GetService("RunService")).IsClient())
                throw new InvalidOperationException("CurrentCamera is client-only.");
            workspace.SetCurrentCamera(Read(state, 3) switch
            {
                null => null,
                Camera camera => camera,
                _ => throw new ArgumentException("Expected Camera or nil."),
            });
            return true;
        }
        if (instance is not Camera target) return false;
        switch (property)
        {
            case "CFrame": target.CFrame = CFrameBindings.Read(state, 3); break;
            case "Focus": target.Focus = CFrameBindings.Read(state, 3); break;
            case "FieldOfView": target.FieldOfView = Number(state, 3); break;
            case "CameraType":
                target.CameraType = Read(state, 3) switch
                {
                    GuiEnum value => value,
                    string name => GuiEnum.From("CameraType", name),
                    _ => throw new ArgumentException("Expected Enum.CameraType."),
                };
                break;
            case "CameraSubject":
                target.CameraSubject = Read(state, 3) switch
                {
                    null => null,
                    Instance subject => subject,
                    _ => throw new ArgumentException("Expected Instance."),
                };
                break;
            case "ViewportSize": throw new InvalidOperationException("ViewportSize is read-only.");
            default: return false;
        }
        return true;
    }
}
