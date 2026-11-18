using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Woorf.Engine.Datatypes;
using static Woorf.Engine.Scripting.NativeMethods;

namespace Woorf.Engine.Scripting;

internal sealed partial class BackendBindings
{
    internal int CallFont(nint state, string operation) => Push(state, operation switch
    {
        "font_new" => CreateFont(state, Text(state, 1)),
        "font_fromName" => CreateFont(state, FontFace.FamilyPath(Text(state, 1))),
        "font_fromId" => CreateFont(state, "rbxassetid://" + Number(state, 1).ToString(CultureInfo.InvariantCulture)),
        "font_fromEnum" => FontFace.FromEnum(Read(state, 1) as GuiEnum ?? throw new ArgumentException("Expected Enum.Font.")),
        "font_get" => ReadFontMember(state),
        _ => throw new InvalidOperationException("Unsupported Font operation.")
    });

    private FontFace CreateFont(nint state, string family) =>
        new(family, FontOption(state, 2, "FontWeight", 400), FontOption(state, 3, "FontStyle", 0));

    private int FontOption(nint state, int index, string type, int fallback) => Read(state, index) switch
    {
        null => fallback,
        GuiEnum value when value.EnumType == type => value.Value,
        _ => throw new ArgumentException($"Expected Enum.{type}.")
    };

    private object ReadFontMember(nint state)
    {
        var font = FontValueCodec.Read(state, 1);
        return Text(state, 2) switch
        {
            "Family" => font.Family, "Bold" => font.Bold,
            "Weight" => GuiEnum.From("FontWeight", GuiEnums.Definitions["FontWeight"][(font.Weight / 100) - 1]),
            "Style" => GuiEnum.From("FontStyle", font.Style == 0 ? "Normal" : "Italic"),
            _ => throw new InvalidOperationException("Unsupported Font member.")
        };
    }
}

internal static class FontValueCodec
{
    internal static FontFace Read(nint state, int index)
    {
        var data = woorf_get_font(state, index, out var length, out var weight, out var style);
        if (data == 0) throw new ArgumentException("Expected Font.");
        return new(Marshal.PtrToStringUTF8(data, checked((int)length))!, weight, style);
    }

    internal static void Push(nint state, FontFace font)
    {
        var family = Encoding.UTF8.GetBytes(font.Family);
        woorf_push_font(state, family, (nuint)family.Length, font.Weight, font.Style);
    }
}
