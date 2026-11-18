using Woorf.Engine.Datatypes;
using Woorf.Engine.Events;

namespace Woorf.Engine.DataModel;

public sealed class UserInputService : Instance
{
    private readonly HashSet<string> keys=[];
    public ScriptSignal InputBegan { get; }=new();
    public ScriptSignal InputEnded { get; }=new();
    internal UserInputService():base("UserInputService") { }
    public bool IsKeyDown(GuiEnum key)
    {
        if(key.EnumType!="KeyCode") throw new InvalidOperationException("Expected Enum.KeyCode.");
        return keys.Contains(key.Name);
    }
    public void SetKey(string key,bool isDown,bool isProcessed)
    {
        GuiEnum.From("KeyCode",key);
        if(!(isDown?keys.Add(key):keys.Remove(key))) return;
        var input=new GuiInputObject(default,default,"Keyboard",isDown?"Begin":"End",key);
        (isDown?InputBegan:InputEnded).Fire(input,isProcessed);
    }
    public void ReleaseKeys() { foreach(var key in keys.ToArray()) SetKey(key,false,false); }
}
