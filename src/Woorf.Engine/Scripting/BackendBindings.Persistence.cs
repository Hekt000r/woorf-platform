using System.Text.Json;
using Woorf.Engine.DataModel;
using Woorf.Engine.Persistence;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    private bool isUpdating;
    private readonly Dictionary<string, Instance> storeHandles = [];
    private readonly Dictionary<Instance, LocalDataStore> stores = [];
    private int CallStore(nint state, Instance instance, string method)
    {
        if (isUpdating) throw new InvalidOperationException("DataStore operations cannot be nested inside UpdateAsync.");
        if (method == "GetDataStore")
        {
            if (((RunService)game.GetService("RunService")).IsClientRuntime) throw new InvalidOperationException("DataStoreService is server-only.");
            var name = Text(state, 2);
            var scopeValue = Read(state, 3);
            if (scopeValue is not null and not string) throw new InvalidOperationException("Expected string scope.");
            var scope = scopeValue as string ?? "global";
            if (name.Length is < 1 or > 50 || scope.Length is < 1 or > 50) throw new InvalidOperationException("Invalid store name or scope.");
            var identifier = JsonSerializer.Serialize(new[] { name, scope });
            if (!storeHandles.TryGetValue(identifier, out var handle))
            {
                handle = Instance.CreateRuntime("GlobalDataStore"); handle.Name = name;
                storeHandles[identifier] = handle;
                stores[handle] = new LocalDataStore(game.DataDirectory, name, scope);
            }
            return Push(state, handle);
        }
        var key = Text(state, 2);
        if (key.Length is < 1 or > 50) throw new InvalidOperationException("Invalid data store key.");
        var store = stores[instance];
        switch (method)
        {
            case "GetAsync": return PushStored(state, store.Read(key));
            case "RemoveAsync": return PushStored(state, store.Remove(key));
            case "SetAsync":
                var value = BackendJson.ToJsonValue(Read(state, 3));
                if (value is null) throw new InvalidOperationException("SetAsync does not accept nil.");
                store.Set(key, JsonSerializer.SerializeToElement(value)); return 0;
            case "UpdateAsync": return UpdateStore(state, store, key);
            case "IncrementAsync": return Push(state, store.Increment(key, Read(state, 3) is null ? 1 : Number(state, 3)));
            default: throw new InvalidOperationException("Unsupported data store method.");
        }
    }
    private int UpdateStore(nint state, LocalDataStore store, string key)
    {
        if (woorf_kind(state, 3) != 7) throw new InvalidOperationException("Expected transform function.");
        var top = woorf_get_top(state);
        isUpdating = true;
        try
        {
            var result = store.Update(key, old =>
            {
                try
                {
                    PushStored(state, old);
                    if (woorf_transform(state, 3) != 0) throw new InvalidOperationException(Text(state, -1));
                    var value = BackendJson.ToJsonValue(Read(state, -1));
                    return value is null ? null : JsonSerializer.SerializeToElement(value);
                }
                finally { woorf_set_top(state, top); }
            });
            return PushStored(state, result);
        }
        finally { isUpdating = false; }
    }

    private int PushStored(nint state, JsonElement? value) => Push(state, value is JsonElement element ? BackendJson.FromJson(element) : null);
}
