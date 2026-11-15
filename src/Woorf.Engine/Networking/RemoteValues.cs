using Woorf.Engine.DataModel;
using Woorf.Engine.Scripting;

namespace Woorf.Engine.Networking;

internal static class RemoteValues
{
    internal static object?[] Translate(object?[] arguments, Func<Instance, Instance?> translate) =>
        arguments.Select(value => TranslateValue(value, translate)).ToArray();

    private static object? TranslateValue(object? value, Func<Instance, Instance?> translate) => value switch
    {
        Instance instance => translate(instance),
        LuaTableValue table => new LuaTableValue(table.Entries.Select(entry =>
            new KeyValuePair<object, object?>(entry.Key, TranslateValue(entry.Value, translate))).ToArray()),
        _ => value,
    };
}
