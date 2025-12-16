using System.Text.Json.Serialization;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, List<TraceSpan>>))]
internal partial class
    AppJson
    :  JsonSerializerContext;