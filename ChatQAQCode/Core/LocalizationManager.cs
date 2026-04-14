using System;
using System.Text.Json;
using MegaCrit.Sts2.Core.Localization;

namespace ChatQAQ.ChatQAQCode.Core;

public class LocalizationManager
{
    private static readonly Lazy<LocalizationManager> _instance = new(() => new LocalizationManager());
    public static LocalizationManager Instance => _instance.Value;

    public string CurrentLanguage { get; private set; } = "zhs";

    public Dictionary<string, string> UILocalization { get; private set; } = new();
    public Dictionary<string, string> MessagesLocalization { get; private set; } = new();

    private const string UiTable = "chatqaq_ui";
    private const string MessagesTable = "chatqaq_messages";

    private LocalizationManager() { }

    public void LoadLocalization()
    {
        if (LocManager.Instance == null)
        {
            MainFile.Logger.Warn("LocManager.Instance is null, will load localization later");
            return;
        }

        var lang = LocManager.Instance.Language;
        if (!string.IsNullOrEmpty(lang))
        {
            CurrentLanguage = lang;
        }

        LoadLocalizationFromFile(UiTable, UILocalization);
        LoadLocalizationFromFile(MessagesTable, MessagesLocalization);

        MainFile.Logger.Info($"LocalizationManager initialized with language: {CurrentLanguage}");
    }

    private void LoadLocalizationFromFile(string tableName, Dictionary<string, string> target)
    {
        target.Clear();

        string path = $"res://ChatQAQ/localization/{CurrentLanguage}/{tableName}.json";

        if (!Godot.FileAccess.FileExists(path))
        {
            MainFile.Logger.Warn($"Localization file not found: {path}");
            return;
        }

        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            MainFile.Logger.Warn($"Failed to open localization file: {Godot.FileAccess.GetOpenError()}");
            return;
        }

        string jsonContent = file.GetAsText();
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    target[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to parse localization file {tableName}: {ex.Message}");
        }
    }

    public string GetUI(string key)
    {
        if (UILocalization.TryGetValue(key, out var value))
        {
            return value;
        }
        return key;
    }

    public string GetMessage(string key)
    {
        if (MessagesLocalization.TryGetValue(key, out var value))
        {
            return value;
        }
        return key;
    }

    public string GetUIFormatted(string key, params object[] args)
    {
        string template = GetUI(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    public string GetMessageFormatted(string key, params object[] args)
    {
        string template = GetMessage(key);
        try
        {
            return string.Format(template, args);
        }
        catch
        {
            return template;
        }
    }

    public void SetLanguage(string language)
    {
        if (string.IsNullOrEmpty(language) || language == CurrentLanguage)
        {
            return;
        }

        CurrentLanguage = language;
        LocManager.Instance.SetLanguage(language);
        LoadLocalization();
    }
}
