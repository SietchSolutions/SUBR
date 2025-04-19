using System;
using System.IO;
using Newtonsoft.Json.Linq;

public static class ConfigHelper
{
    private static readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logConfig.json");
    private static JObject config;

    static ConfigHelper()
    {
        try
        {
            if (!File.Exists(configPath))
                config = new JObject();
            else
                config = JObject.Parse(File.ReadAllText(configPath));
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error loading config: " + ex.Message);
            config = new JObject();
        }
    }

    // 🔽 GETTERS

    public static string GetStationTemplateCsvPath() => config["stationTemplateCsvPath"]?.ToString() ?? "";
   
    
    public static string GetLogFilePath() => config["logFileDirectory"]?.ToString() ?? "";
    

    public static bool GetSkipSplash() => config["skipSplash"]?.ToObject<bool>() ?? false;

    // 🔼 SETTERS

    public static void SetValue(string key, object value)
    {
        config[key] = JToken.FromObject(value);
    }

    public static void Save()
    {
        File.WriteAllText(configPath, config.ToString());
    }
}
