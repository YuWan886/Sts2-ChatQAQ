using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Platform;
using ChatQAQ.ChatQAQCode.Core;
using ChatQAQ.ChatQAQCode.Data;
using ChatQAQ.ChatQAQCode.UI;
using ChatQAQ.ChatQAQCode.Networking;

namespace ChatQAQ.ChatQAQCode;

[ModInitializer(nameof(Initialize))]
public static class MainFile
{
    public const string ModId = "ChatQAQ";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static ChatInputBox? ChatInputBox { get; private set; }
    public static SettingsPanel? SettingsPanel { get; private set; }
    public static HistoryPanel? HistoryPanel { get; private set; }

    public static void Initialize()
    {
        try
        {
            Logger.Info("ChatQAQ mod initializing...");

            Harmony harmony = new(ModId);
            harmony.PatchAll();

            ConfigManager.Instance.Load();
            LocalizationManager.Instance.LoadLocalization();

            Logger.Info("ChatQAQ mod initialized successfully!");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to initialize ChatQAQ: {ex}");
        }
    }

    public static void CreateUI(Node parent)
    {
        if (ChatInputBox != null) return;

        try
        {
            Logger.Info("Creating ChatQAQ UI...");

            ChatInputBox = new ChatInputBox();
            parent.AddChild(ChatInputBox);

            SettingsPanel = new SettingsPanel();
            parent.AddChild(SettingsPanel);
            SettingsPanel.Visible = false;

            HistoryPanel = new HistoryPanel();
            parent.AddChild(HistoryPanel);
            HistoryPanel.Visible = false;

            ChatInputBox.OpenSettings += () => SettingsPanel.Show();
            ChatInputBox.OpenHistory += () => HistoryPanel.Show();

            SettingsPanel.CloseRequested += () => SettingsPanel.Hide();
            HistoryPanel.CloseRequested += () => HistoryPanel.Hide();

            HotkeyManager.Instance.OnQuickSendTriggered += OnQuickSendTriggered;

            Logger.Info("ChatQAQ UI created successfully!");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to create ChatQAQ UI: {ex}");
            ChatInputBox?.QueueFree();
            SettingsPanel?.QueueFree();
            HistoryPanel?.QueueFree();
            ChatInputBox = null;
            SettingsPanel = null;
            HistoryPanel = null;
        }
    }

    public static void SetLocalPlayerFromRunState(RunState runState)
    {
        try
        {
            if (runState == null) return;

            foreach (var player in runState.Players)
            {
                if (LocalContext.IsMe(player))
                {
                    var playerName = PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
                    var characterId = player.Character?.Id?.Entry ?? "";

                    var playerInfo = new PlayerInfo(
                        player.NetId.ToString(),
                        playerName,
                        characterId,
                        true
                    );

                    ChatManager.Instance.SetLocalPlayer(playerInfo);
                    Logger.Info($"Local player set: {playerName} ({characterId})");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to set local player: {ex}");
        }
    }

    private static void OnQuickSendTriggered(Vector2 position)
    {
        try
        {
            QuickSendManager.Instance.SendQuickInfoAtPosition(position);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to send quick info: {ex}");
        }
    }
}

[HarmonyPatch(typeof(NGame))]
public static class NGamePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NGame._Ready))]
    public static void Postfix_Ready(NGame __instance)
    {
        MainFile.Logger.Info("NGame._Ready called, creating ChatQAQ UI...");

        LocalizationManager.Instance.LoadLocalization();

        MainFile.CreateUI(__instance);
    }
}

[HarmonyPatch(typeof(NGame))]
public static class NGameInputPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NGame._Input))]
    public static void Postfix_Input(NGame __instance, InputEvent inputEvent)
    {
        HotkeyManager.Instance.ProcessInput(inputEvent);
    }
}

[HarmonyPatch(typeof(NRun))]
public static class NRunPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(NRun._Ready))]
    public static void Postfix_Ready(NRun __instance)
    {
        MainFile.Logger.Info("NRun._Ready called, setting local player...");

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null)
        {
            MainFile.SetLocalPlayerFromRunState(runState);
            ChatManager.Instance.StartNewSession();
            ChatNetworkManager.Instance.Initialize();
        }
    }
}
