using System.Text.Json;
using System.Text.Json.Serialization;
using Woorf.Engine.Datatypes;

internal sealed class GuiVectorJsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) =>
        throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X); writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}
