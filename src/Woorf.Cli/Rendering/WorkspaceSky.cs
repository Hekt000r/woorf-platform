using Woorf.Engine.DataModel;
using Woorf.Graphics;

internal sealed class WorkspaceSky : IDisposable
{
    private readonly DataModel game;
    private bool isDirty = true;
    private Skybox? captured;
    internal WorkspaceSky(DataModel game)
    {
        this.game = game;
        game.DescendantMutated += OnMutation;
    }
    private void OnMutation(Instance instance, string property)
    {
        if (instance is Sky || instance.ClassName == "Lighting" || property == "Parent") isDirty = true;
    }
    internal Skybox? Capture()
    {
        if (!isDirty) return captured;
        isDirty = false;
        var sky = game.FindService("Lighting")?.GetChildren().OfType<Sky>().FirstOrDefault();
        captured = sky is null ? null : new Skybox(Sky.FaceProperties.Select(sky.GetTexture),
            new(sky.SkyboxOrientation.X, sky.SkyboxOrientation.Y, sky.SkyboxOrientation.Z));
        return captured;
    }
    public void Dispose() => game.DescendantMutated -= OnMutation;
}
