using Woorf.Engine.DataModel;
using Woorf.Engine.Physics;

namespace Woorf.Engine.Characters;

internal sealed class NpcWorld : IDisposable
{
    private readonly DataModel.DataModel game;
    private readonly PhysicsWorld physics;
    private readonly Dictionary<Model,(Part Root,Humanoid Humanoid,CharacterController Controller)> entries=[];
    private bool isDirty=true;

    internal NpcWorld(DataModel.DataModel game,PhysicsWorld physics)
    {
        this.game=game; this.physics=physics; game.DescendantMutated+=OnMutation;
    }

    private void OnMutation(Instance instance,string property)
    {
        if(property is "Parent" or "PrimaryPart" or "Character") isDirty=true;
    }

    internal void Synchronize()
    {
        if(!isDirty) return;
        isDirty=false;
        var players=game.FindService("Players") as Players;
        var models=game.Workspace.GetDescendants().OfType<Model>().Where(model=>
            model.PrimaryPart is Part && model.GetChildren().OfType<Humanoid>().FirstOrDefault() is Humanoid
            && players?.GetPlayerFromCharacter(model) is null).ToHashSet();
        foreach(var (model,entry) in entries.ToArray())
            if(!models.Contains(model)||model.PrimaryPart!=entry.Root||model.GetChildren().OfType<Humanoid>().FirstOrDefault()!=entry.Humanoid)
            { entry.Controller.Dispose(); entries.Remove(model); }
        foreach(var model in models)
        {
            if(entries.ContainsKey(model)) continue;
            var root=(Part)model.PrimaryPart!; var humanoid=(Humanoid)model.GetChildren().OfType<Humanoid>().FirstOrDefault()!;
            entries.Add(model,(root,humanoid,new CharacterController(physics,root,humanoid)));
        }
    }

    public void Dispose()
    {
        game.DescendantMutated-=OnMutation;
        foreach(var entry in entries.Values) entry.Controller.Dispose();
        entries.Clear();
    }
}

