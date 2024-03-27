using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MemeIndex_Core.Utils;

public class JsonIO<T> : JsonOptions where T : new()
{
    private T? _data;

    private readonly string _path;

    public JsonIO(string path)
    {
        _path = path;
    }

    public T LoadData()
    {
        var file = new FileInfo(_path);
        if (file is { Exists: true, Length: > 0 })
        {
            using var stream = File.OpenText(_path);
            using var reader = new JsonTextReader(stream);
            _data = Serializer.Deserialize<T>(reader);
            return _data!;
        }

        _data = new T();
        SaveData();
        return _data;
    }

    public void SaveData()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.CreateText(_path);
        using var writer = new JsonTextWriter(stream);
        Serializer.Serialize(writer, _data);
    }
}

public class JsonOptions
{
    protected static readonly JsonSerializer Serializer = new()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new KebabCaseNamingStrategy()
        },
        DefaultValueHandling = DefaultValueHandling.Include,
    };
}