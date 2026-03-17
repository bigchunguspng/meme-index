using System.Text.Json;
using System.Text.Json.Serialization;
using MemeIndex.Core.Search;
using SixLabors.ImageSharp;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(SearchResponse))]
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
        Options.Converters.Add(new JsonConverter_File_UI_SoA_Slice());
    }
}

public sealed class JsonConverter_File_UI_SoA_Slice : JsonConverter<File_UI_SoA_Slice>
{
    public override File_UI_SoA_Slice Read
        (ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
    public override void Write
        (Utf8JsonWriter writer, File_UI_SoA_Slice value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        var pagination = value.GetPagination();
        var off   = pagination.o;
        var len   = pagination.o + pagination.r;
        var files = value.Files;
        for (var i = off; i < len; i++)
        {
            writer.WriteStartObject();
            writer.WriteNumber("i", files.I[i]);
            writer.WriteNumber("d", files.D[i]);
            writer.WriteString("n", files.N[i]);
            writer.WriteNumber("s", files.S[i]);
            writer.WriteString("m", files.M[i]);
            writer.WriteStartObject("x");
            writer.WriteNumber("w", files.X[i].Width);
            writer.WriteNumber("h", files.X[i].Height);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}