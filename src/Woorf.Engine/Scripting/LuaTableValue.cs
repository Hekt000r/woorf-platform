namespace Woorf.Engine.Scripting;

internal sealed record LuaTableValue(IReadOnlyList<KeyValuePair<object, object?>> Entries);
