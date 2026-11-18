using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.DataModel;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed class LuaValueCodec(Func<Instance?, int> getInstanceId, Func<int, Instance?> findInstance)
{
    internal object? ReadValue(nint state, int index) => Read(state, index, [], 0, false);

    internal object?[] ReadArguments(nint state, int first)
    {
        var count = woorf_get_top(state) - first + 1;
        var values = new object?[count];
        for (var index = 0; index < count; index++)
            values[index] = Read(state, first + index, [], 0);
        return values;
    }

    internal int Invoke(nint handle, int reference, object?[] arguments)
    {
        PushArguments push = state =>
        {
            try
            {
                foreach (var argument in arguments) Push(state, argument);
                return arguments.Length;
            }
            catch (Exception error)
            {
                PushString(state, error.Message);
                return -1;
            }
        };
        var result = OperatingSystem.IsBrowser() ? NativeCallbackLease.InvokeValues(handle, reference, push)
            : woorf_invoke_values(handle, reference, push);
        GC.KeepAlive(push);
        return result;
    }

    private object? Read(nint state, int index, HashSet<nint> ancestors, int depth, bool shouldAllowFunctions = true)
    {
        if (depth > 64) throw new InvalidOperationException("Remote values exceed the supported nesting depth.");
        return woorf_value_kind(state, index) switch
        {
            0 => null,
            1 => woorf_get_bool(state, index) != 0,
            2 => ReadString(state, index),
            3 => findInstance(woorf_get_id(state, index)),
            7 => shouldAllowFunctions ? null : throw new InvalidOperationException("Functions are not supported values."),
            8 => ReadNumber(state, index),
            9 => ReadTable(state, index, ancestors, depth, shouldAllowFunctions),
            10 => Vector3Bindings.Read(state, index),
            11 => CFrameBindings.Read(state, index),
            12 => ReadColor(state, index),
            13 => PhysicsBindings.ReadMaterial(state, index),
            >= 14 and <= 17 => GuiValueCodec.Read(state, index, woorf_value_kind(state, index) - 14),
            _ => throw new InvalidOperationException("Unsupported remote argument type."),
        };
    }

    private static Color3 ReadColor(nint state, int index)
    {
        if (woorf_get_color3(state, index, out var red, out var green, out var blue) == 0)
            throw new InvalidOperationException("Expected Color3.");
        return new(red, green, blue);
    }

    private LuaTableValue ReadTable(nint state, int index, HashSet<nint> ancestors, int depth, bool shouldAllowFunctions = true)
    {
        var originalTop = woorf_get_top(state);
        var absoluteIndex = index > 0 ? index : originalTop + index + 1;
        var identity = woorf_table_identity(state, absoluteIndex);
        if (!ancestors.Add(identity)) throw new InvalidOperationException("Cyclic remote tables are not supported.");
        var entries = new List<KeyValuePair<object, object?>>();
        try
        {
            woorf_push_nil(state);
            while (woorf_table_next(state, absoluteIndex) != 0)
            {
                var key = ReadKey(state, -2);
                var value = Read(state, -1, ancestors, depth + 1, shouldAllowFunctions);
                entries.Add(new(key, value));
                woorf_set_top(state, woorf_get_top(state) - 1);
            }
            return new(entries);
        }
        finally
        {
            woorf_set_top(state, originalTop);
            ancestors.Remove(identity);
        }
    }

    private static object ReadKey(nint state, int index) => woorf_value_kind(state, index) switch
    {
        2 => ReadString(state, index),
        8 => ReadNumber(state, index),
        _ => throw new InvalidOperationException("Remote table keys must be strings or numbers."),
    };

    internal void Push(nint state, object? value) => Push(state, value, 0);

    private void Push(nint state, object? value, int depth)
    {
        if (depth > 64) throw new InvalidOperationException("Remote values exceed the supported nesting depth.");
        switch (value)
        {
            case null: woorf_push_nil(state); break;
            case bool boolean: woorf_push_bool(state, boolean ? 1 : 0); break;
            case double number: woorf_push_number(state, number); break;
            case int number: woorf_push_number(state, number); break;
            case long number: woorf_push_number(state, number); break;
            case string text: PushString(state, text); break;
            case Instance instance: woorf_push_instance(state, getInstanceId(instance)); break;
            case Vector3 vector: Vector3Bindings.Push(state, vector); break;
            case CFrame frame: CFrameBindings.Push(state, frame); break;
            case PhysicalProperties material: woorf_push_physical_properties(state, material.GetComponents()); break;
            case Color3 color: woorf_push_color3(state, color.R, color.G, color.B); break;
            case Vector2 or UDim or UDim2 or GuiEnum: GuiValueCodec.Push(state, value); break;
            case GuiInputObject input: PushInput(state, input, depth); break;
            case LuaTableValue table: PushTable(state, table, depth); break;
            default: throw new InvalidOperationException("Unsupported callback argument type.");
        }
    }

    private void PushTable(nint state, LuaTableValue table, int depth)
    {
        woorf_new_table(state);
        var index = woorf_get_top(state);
        foreach (var entry in table.Entries)
        {
            Push(state, entry.Key, depth + 1);
            Push(state, entry.Value, depth + 1);
            woorf_raw_set(state, index);
        }
    }

    private void PushInput(nint state, GuiInputObject input, int depth)
    {
        var properties = new Dictionary<string, object>
        {
            ["Position"] = input.Position, ["Delta"] = input.Delta,
            ["UserInputType"] = GuiEnum.From("UserInputType", input.InputType),
            ["UserInputState"] = GuiEnum.From("UserInputState", input.InputState),
            ["KeyCode"] = GuiEnum.From("KeyCode", input.KeyCode),
        };
        PushTable(state, new(properties.Select(entry => new KeyValuePair<object, object?>(entry.Key, entry.Value)).ToArray()), depth);
        woorf_wrap_input_object(state);
    }

    private static double ReadNumber(nint state, int index)
    {
        woorf_get_number(state, index, out var value);
        return value;
    }

    private static string ReadString(nint state, int index)
    {
        var pointer = woorf_get_string(state, index, out var length);
        return Marshal.PtrToStringUTF8(pointer, checked((int)length))!;
    }

    private static void PushString(nint state, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        woorf_push_string(state, bytes, (nuint)bytes.Length);
    }
}
