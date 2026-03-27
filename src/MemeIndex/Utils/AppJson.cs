using System.Text.Json;
using System.Text.Json.Serialization;
using MemeIndex.Core.Indexing.Selection;
using MemeIndex.Core.Search;
using SixLabors.ImageSharp;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(DirectoryResponse))]
[JsonSerializable(typeof(Dictionary<string, List<TraceSpan>>))]
internal partial class
    AppJson
    :  JsonSerializerContext
{
    private new static readonly JsonSerializerOptions Options;

    static AppJson()
    {
        Options = new JsonSerializerOptions
        {
            TypeInfoResolver = Default,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };
        Options.Converters.Add(new JsonConverter_Size());
    }
}

public sealed class JsonConverter_Size : JsonConverter<Size>
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
        writer.WriteNumber("w", value.Width);
        writer.WriteNumber("h", value.Height);
        writer.WriteEndObject();
    }
}