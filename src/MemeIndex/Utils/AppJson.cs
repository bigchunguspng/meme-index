using System.Text.Json;
using System.Text.Json.Serialization;
using MemeIndex.Core.Search;
using SixLabors.ImageSharp;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(IEnumerable<File_UI>))]
[JsonSerializable(typeof(Dictionary<string, List<TraceSpan>>))]
internal partial class
    AppJson
    :  JsonSerializerContext
{
    public new static readonly JsonSerializerOptions Options;

    static AppJson()
    {
        Options = new JsonSerializerOptions
        {
            TypeInfoResolver = Default,
            PropertyNamingPolicy   = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        Options.Converters.Add(new SizeConverter());
    }
}

public sealed class SizeConverter : JsonConverter<Size>
{
    public override Size Read
        (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write
        (Utf8JsonWriter writer, Size value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("width", value.Width);
        writer.WriteNumber("height", value.Height);
        writer.WriteEndObject();
    }
}
