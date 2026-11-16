using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public abstract partial class BasePart
{
    private Vector3 linearVelocity, angularVelocity;
    private bool canCollide = true;
    private PhysicalProperties? customPhysicalProperties;
    public Vector3 AssemblyLinearVelocity
    {
        get => linearVelocity;
        set { ValidateVelocity(value); if (value == linearVelocity) return; linearVelocity = value; FirePropertyChanged("AssemblyLinearVelocity"); }
    }
    public Vector3 AssemblyAngularVelocity
    {
        get => angularVelocity;
        set { ValidateVelocity(value); if (value == angularVelocity) return; angularVelocity = value; FirePropertyChanged("AssemblyAngularVelocity"); }
    }
    public bool CanCollide
    {
        get => canCollide;
        set { if (canCollide == value) return; canCollide = value; FirePropertyChanged("CanCollide"); }
    }
    public PhysicalProperties? CustomPhysicalProperties
    {
        get => customPhysicalProperties;
        set { if (customPhysicalProperties == value) return; customPhysicalProperties = value; FirePropertyChanged("CustomPhysicalProperties"); }
    }
    private static void ValidateVelocity(Vector3 value)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
            throw new ArgumentException("Expected finite velocity components.");
    }
    private static bool HasPhysicsProperty(string name) => name is "AssemblyLinearVelocity" or "AssemblyAngularVelocity" or "CanCollide" or "CustomPhysicalProperties";
    private void CopyPhysicsTo(BasePart part)
    {
        part.linearVelocity = linearVelocity; part.angularVelocity = angularVelocity;
        part.canCollide = canCollide; part.customPhysicalProperties = customPhysicalProperties;
    }
    internal void ApplyPhysicsState(CFrame frame, Vector3 linear, Vector3 angular)
    {
        var previousFrame = cframe; var previousLinear = linearVelocity; var previousAngular = angularVelocity;
        cframe = frame; linearVelocity = linear; angularVelocity = angular;
        if (previousFrame != frame) FirePropertyChanged("CFrame");
        if (previousFrame.Position != frame.Position) FirePropertyChanged("Position");
        if (previousLinear != linear) FirePropertyChanged("AssemblyLinearVelocity");
        if (previousAngular != angular) FirePropertyChanged("AssemblyAngularVelocity");
    }
}
