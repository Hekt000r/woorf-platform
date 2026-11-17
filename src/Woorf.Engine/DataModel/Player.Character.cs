using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed partial class Player
{
    private Model? character;
    public ScriptSignal CharacterAdded { get; } = new();
    public ScriptSignal CharacterRemoving { get; } = new();

    public Model? Character
    {
        get => character;
        internal set
        {
            if (character == value) return;
            if (character is not null) CharacterRemoving.Fire(character);
            character = value;
            FirePropertyChanged("Character");
            if (character is not null) CharacterAdded.Fire(character);
        }
    }
}
