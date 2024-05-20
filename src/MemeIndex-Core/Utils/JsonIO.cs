using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MemeIndex_Core.Utils;

public class JsonIO<T>(string path) : JsonSerializerOptions where T : new()
{
    private T? _data;

    public T LoadData()
    {
        if (_data is not null)
        {
            return _data;
        }

        var file = new FileInfo(path);
        if (file is { Exists: true, Length: > 0 })
        {
            using var stream = File.OpenText(path);
            using var reader = new JsonTextReader(stream);
            _data = Serializer.Deserialize<T>(reader)!; // 0.17 sec
        }
        else
        {
            using var memory = new MemoryStream("{}"u8.ToArray());
            using var stream = new StreamReader(memory);
            using var reader = new JsonTextReader(stream);
            _data = Serializer.Deserialize<T>(reader)!;
        }

        SaveData(); // 0.03 sec
        return _data;
    }

    public void SaveData()
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.CreateText(path);
        using var writer = new JsonTextWriter(stream);
        Serializer.Serialize(writer, _data);
    }
}

public class JsonSerializerOptions
{
    protected static readonly JsonSerializer Serializer = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new KebabCaseNamingStrategy()
        },
        DefaultValueHandling = DefaultValueHandling.Populate
    };
}