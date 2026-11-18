using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private sealed class TweenInstance(Instance target) : Instance("Tween")
    {
        internal Instance Target { get; } = target;
        internal GuiEnum State { get; private set; } = GuiEnum.From("PlaybackState", "Begin");
        internal ScriptSignal Completed { get; } = new();

        internal void SetState(GuiEnum next)
        {
            if (State == next) return;
            State = next;
            FirePropertyChanged("PlaybackState");
            if (next.Name is "Completed" or "Cancelled") Completed.Fire(next);
        }

        protected override bool HasProperty(string property) =>
            property is "PlaybackState" or "Instance" || base.HasProperty(property);
    }

    private bool TryReadTween(nint state, Instance instance, string property)
    {
        if (instance.ClassName == "TweenService" && property is "Create" or "GetValue" or "_CreateTween")
        {
            woorf_push_method(state, property == "_CreateTween" ? property : "Tween" + property);
            return true;
        }
        if (instance is not TweenInstance tween) return false;
        switch (property)
        {
            case "Instance": Push(state, tween.Target); break;
            case "PlaybackState": Push(state, tween.State); break;
            case "Completed": signals.PushSignal(state, tween.Completed); break;
            case "Play" or "Pause" or "Cancel": woorf_push_method(state, "Tween" + property); break;
            case "_SetTweenState": woorf_push_method(state, property); break;
            default: return false;
        }
        return true;
    }

    private bool TryCallTween(nint state, Instance instance, string method, out int result)
    {
        result = 0;
        if (instance.ClassName == "TweenService" && method == "_CreateTween")
        {
            var tween = new TweenInstance(Require(state, 2)) { Archivable = false };
            tween.GetSignal("Destroying").Connect(_ => tween.Completed.Close());
            result = Push(state, tween);
            return true;
        }
        if (instance is not TweenInstance receiver || method != "_SetTweenState") return false;
        receiver.SetState(Read(state, 2) is GuiEnum { EnumType: "PlaybackState" } playback ? playback
            : throw new ArgumentException("Expected Enum.PlaybackState."));
        return true;
    }
}
