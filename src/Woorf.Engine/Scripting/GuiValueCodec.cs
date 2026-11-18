using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal static class GuiValueCodec
{
    internal static object Read(nint state, int index, int kind)
    {
        var components = new double[4];
        if (woorf_get_gui_value(state, index, kind, components) == 0) throw new InvalidOperationException("Invalid GUI datatype.");
        return kind switch
        {
            0 => new Vector2((float)components[0], (float)components[1]),
            1 => new UDim((float)components[0], checked((int)components[1])),
            2 => new UDim2((float)components[0], checked((int)components[1]), (float)components[2], checked((int)components[3])),
            3 => ReadEnum(checked((int)components[0]) - 1, checked((int)components[1])),
            _ => throw new InvalidOperationException("Invalid GUI datatype."),
        };
    }

    private static GuiEnum ReadEnum(int type, int value)
    {
        if (type < 0 || type >= GuiEnums.Types.Length) throw new InvalidOperationException("Invalid GUI enum.");
        var name = GuiEnums.Types[type];
        var items = GuiEnums.Definitions[name];
        if (value < 0 || value >= items.Length) throw new InvalidOperationException("Invalid GUI enum value.");
        return GuiEnum.From(name, items[value]);
    }

    internal static void Push(nint state, object value)
    {
        switch (value)
        {
            case Vector2 vector: woorf_push_gui_value(state, 0, vector.X, vector.Y, 0, 0); break;
            case UDim dimension: woorf_push_gui_value(state, 1, dimension.Scale, dimension.Offset, 0, 0); break;
            case UDim2 dimensions: woorf_push_gui_value(state, 2, dimensions.X.Scale, dimensions.X.Offset, dimensions.Y.Scale, dimensions.Y.Offset); break;
            case GuiEnum item: woorf_push_gui_value(state, 3, Array.IndexOf(GuiEnums.Types, item.EnumType) + 1,
                Array.IndexOf(GuiEnums.Definitions[item.EnumType], item.Name), 0, 0); break;
        }
    }
}
