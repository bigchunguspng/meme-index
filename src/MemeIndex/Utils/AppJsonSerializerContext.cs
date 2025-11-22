using System.Text.Json.Serialization;

namespace MemeIndex.Utils;

[JsonSerializable(typeof(List<string>))]
internal partial class
    AppJsonSerializerContext
    :  JsonSerializerContext;