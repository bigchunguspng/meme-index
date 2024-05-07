using MemeIndex_Core.Utils;

namespace MemeIndex_Core;

public interface IConfigProvider<out T> where T : Config, new()
{
    T GetConfig();
    void SaveChanges();
}

public class ConfigProvider<T> : IConfigProvider<T> where T : Config, new()
{
    private T? _instance;
    private JsonIO<T> ConfigJson { get; } = new(@"config.json");

    public T GetConfig() => _instance ??= ConfigJson.LoadData();
    public void SaveChanges() => ConfigJson.SaveData();
}