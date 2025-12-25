using System.Text.Json.Serialization;
using MemeIndex.DB;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IEnumerable<DB_Tag>))]
[JsonSerializable(typeof(Dictionary<string, List<TraceSpan>>))]
internal partial class
    AppJson
    :  JsonSerializerContext;