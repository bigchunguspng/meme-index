using System.Text.Json.Serialization;
using Trace = MemeIndex.Tools.Logging.Trace;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<string, List<Trace>>))]
internal partial class
    AppJsonSerializerContext
    :  JsonSerializerContext;