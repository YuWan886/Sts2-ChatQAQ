using System;
using System.Text.Json;

namespace ChatQAQ.ChatQAQCode.Data;

public class ConfigManager
{
    private static readonly Lazy<ConfigManager> _instance = new(() => new ConfigManager());
    public static ConfigManager Instance => _instance.Value;

    private const string ConfigFileName = "chat_config.json";

    public ChatConfig CurrentConfig { get; private set; }

    private ConfigManager()
    {
        CurrentConfig = ChatConfig.CreateDefault();
    }

    public void Load()
    {
        string path = GetConfigPath();

        if (!Godot.FileAccess.FileExists(path))
        {
            CurrentConfig = ChatConfig.CreateDefault();
            return;
        }

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            MainFile.Logger.Warn($"Failed to open config file: {Godot.FileAccess.GetOpenError()}");
            CurrentConfig = ChatConfig.CreateDefault();
            return;
        }

        string jsonContent = file.GetAsText();
        try
        {
            var loadedConfig = JsonSerializer.Deserialize<ChatConfig>(jsonContent);
            if (loadedConfig != null)
            {
                CurrentConfig = loadedConfig;
            }
            else
            {
                CurrentConfig = ChatConfig.CreateDefault();
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to parse config file: {ex.Message}");
            CurrentConfig = ChatConfig.CreateDefault();
        }
    }

    public void Save()
    {
        string path = GetConfigPath();

        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(CurrentConfig, options);

            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                MainFile.Logger.Warn($"Failed to open config file for writing: {Godot.FileAccess.GetOpenError()}");
                return;
            }

            file.StoreString(jsonContent);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to save config file: {ex.Message}");
        }
    }

    public void ResetToDefault()
    {
        CurrentConfig = ChatConfig.CreateDefault();
        Save();
    }

    private static string GetConfigPath()
    {
        return $"user://{ConfigFileName}";
    }
}
