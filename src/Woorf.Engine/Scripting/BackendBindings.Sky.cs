using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool TryReadSky(nint state, Instance instance, string property)
    {
        if (instance is not Sky sky) return false;
        if (property == "SkyboxOrientation") { Push(state, sky.SkyboxOrientation); return true; }
        if (!Sky.FaceProperties.Contains(property)) return false;
        Push(state, sky.GetTexture(property));
        return true;
    }
    private bool TryWriteSky(nint state, Instance instance, string property)
    {
        if (instance is not Sky sky) return false;
        if (property == "SkyboxOrientation")
        {
            sky.SkyboxOrientation = Read(state, 3) is Vector3 value ? value : throw new ArgumentException("Expected Vector3.");
            return true;
        }
        if (!Sky.FaceProperties.Contains(property)) return false;
        sky.SetTexture(property, Text(state, 3));
        return true;
    }
}
