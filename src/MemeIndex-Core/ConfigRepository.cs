using MemeIndex_Core.Utils;

namespace MemeIndex_Core;

public static class ConfigRepository
{
    static ConfigRepository()
    {
        ConfigJson = new JsonIO<Config>(@"config.json");
    }

    private static JsonIO<Config> ConfigJson { get; set; }

    public static Config GetConfig() => ConfigJson.LoadData();

    public static void SaveChanges() => ConfigJson.SaveData();
}