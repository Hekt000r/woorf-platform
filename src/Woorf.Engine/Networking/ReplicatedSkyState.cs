using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;

namespace Woorf.Engine.Networking;

internal sealed record ReplicatedSkyState(string[] Faces, Vector3 Orientation)
{
    internal static ReplicatedSkyState? Capture(Instance instance) => instance is Sky sky
        ? new(Sky.FaceProperties.Select(sky.GetTexture).ToArray(), sky.SkyboxOrientation) : null;
    internal void Apply(Instance instance, ReplicatedSkyState? previous)
    {
        if (instance is not Sky sky) return;
        for (var index = 0; index < Faces.Length; index++)
            if (previous is null || previous.Faces[index] != Faces[index]) sky.SetTexture(Sky.FaceProperties[index], Faces[index]);
        if (previous is null || previous.Orientation != Orientation) sky.SkyboxOrientation = Orientation;
    }
    internal WireSkyState Encode() => new(Faces, [Orientation.X, Orientation.Y, Orientation.Z]);
}
internal sealed record WireSkyState(string[] Faces, float[] Orientation)
{
    internal ReplicatedSkyState Decode()
    {
        if (Faces.Length != 6 || Orientation.Length != 3 || Faces.Any(face => face is null))
            throw new InvalidDataException("Invalid sky state.");
        return new(Faces, new(Orientation[0], Orientation[1], Orientation[2]));
    }
}
