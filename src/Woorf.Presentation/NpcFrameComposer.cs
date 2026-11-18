using Woorf.Engine.DataModel;
using Woorf.Graphics;

namespace Woorf.Presentation;

internal sealed class NpcFrameComposer : IDisposable
{
    private readonly DataModel game;
    private readonly Dictionary<Model,CharacterView> views=[];
    private Model[] models=[];
    private RenderItem[] items=[];
    private bool isDirty=true;

    internal NpcFrameComposer(DataModel game) { this.game=game; game.DescendantMutated+=OnMutation; }
    private void OnMutation(Instance instance,string property)
    {
        if(property is "Parent" or "PrimaryPart" or "Character") isDirty=true;
    }

    internal RenderItem[] Capture(RenderItem[] scene,double now)
    {
        if(isDirty)
        {
            var players=game.FindService("Players") as Players;
            models=game.Workspace.GetDescendants().OfType<Model>().Where(model=>model.PrimaryPart is Part
                && model.GetChildren().OfType<Humanoid>().FirstOrDefault() is Humanoid && players?.GetPlayerFromCharacter(model) is null).ToArray();
            foreach(var departed in views.Keys.Except(models).ToArray()) views.Remove(departed);
            foreach(var model in models) views.TryAdd(model,new CharacterView());
            isDirty=false;
        }
        if(models.Length==0) return scene;
        if(items.Length!=scene.Length+models.Length*8) items=new RenderItem[scene.Length+models.Length*8];
        scene.CopyTo(items,0);
        for(var index=0;index<models.Length;index++)
        {
            var model=models[index]; var root=(Part)model.PrimaryPart!;
            var humanoid=(Humanoid)model.GetChildren().OfType<Humanoid>().FirstOrDefault()!;
            views[model].Capture(root,humanoid,now).CopyTo(items,scene.Length+index*8);
        }
        return items;
    }

    public void Dispose() { game.DescendantMutated-=OnMutation; views.Clear(); }
}

