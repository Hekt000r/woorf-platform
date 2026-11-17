using Woorf.Engine.Datatypes;

namespace Woorf.Engine.DataModel;

public sealed class Sky : Instance
{
    public static IReadOnlyList<string> FaceProperties { get; } = Array.AsReadOnly(new[] { "SkyboxRt", "SkyboxLf", "SkyboxUp", "SkyboxDn", "SkyboxBk", "SkyboxFt" });
    private readonly string[] textures = new string[6];
    private Vector3 orientation;

    internal Sky() : base("Sky")
    {
        string[] faces = ["rt", "lf", "up", "dn", "bk", "ft"];
        for (var index = 0; index < faces.Length; index++) textures[index] = $"rbxasset://textures/sky/sky512_{faces[index]}.tex";
    }

    public string GetTexture(string property) => textures[FindFace(property)];
    public void SetTexture(string property, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var index = FindFace(property);
        if (textures[index] == value) return;
        textures[index] = value;
        FirePropertyChanged(property);
    }
    private static int FindFace(string property)
    {
        for (var index = 0; index < FaceProperties.Count; index++)
            if (FaceProperties[index] == property) return index;
        throw new ArgumentException("Unknown sky face.", nameof(property));
    }
    public Vector3 SkyboxOrientation
    {
        get => orientation;
        set
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
                throw new ArgumentException("SkyboxOrientation must be finite.");
            if (orientation == value) return;
            orientation = value;
            FirePropertyChanged("SkyboxOrientation");
        }
    }
    protected override bool HasProperty(string property) =>
        property == "SkyboxOrientation" || FaceProperties.Contains(property) || base.HasProperty(property);
    protected override void CopyPropertiesTo(Instance clone)
    {
        var sky = (Sky)clone;
        textures.CopyTo(sky.textures, 0);
        sky.orientation = orientation;
    }
}
