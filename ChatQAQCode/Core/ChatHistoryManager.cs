using System;
using System.Text.Json;
using ChatQAQ.ChatQAQCode.Data;

namespace ChatQAQ.ChatQAQCode.Core;

public class ChatHistoryManager
{
    private static readonly Lazy<ChatHistoryManager> _instance = new(() => new ChatHistoryManager());
    public static ChatHistoryManager Instance => _instance.Value;

    public List<ChatSession> Sessions { get; private set; } = new List<ChatSession>();
    public List<ChatMessage> CurrentSessionMessages { get; private set; } = new List<ChatMessage>();
    public int MaxMessagesPerSession { get; set; } = 500;

    private ChatSession _currentSession = null!;
    private static readonly string SavePath = "user://chat_history.json";
    private bool _isInitialized = false;

    private ChatHistoryManager() { }

    public void Initialize()
    {
        if (_isInitialized) return;
        LoadFromFile();
        _isInitialized = true;
    }

    public void AddMessage(ChatMessage message)
    {
        if (message == null) return;

        if (_currentSession != null)
        {
            _currentSession.Messages.Add(message);
        }
        else
        {
            CurrentSessionMessages.Add(message);
        }

        TrimOldMessages();
    }

    public void AddSession(ChatSession session)
    {
        if (session == null) return;

        if (_currentSession != null && !_currentSession.IsEnded)
        {
            EndCurrentSession();
        }

        _currentSession = session;
        CurrentSessionMessages = session.Messages;
        Sessions.Add(session);
    }

    public void UpdateSession(ChatSession session)
    {
        if (session == null) return;

        var existingSession = Sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
        if (existingSession != null)
        {
            existingSession.EndTime = session.EndTime;
        }
    }

    public void StartNewSession(ChatSession session)
    {
        AddSession(session);
    }

    public void EndCurrentSession()
    {
        if (_currentSession == null) return;

        _currentSession.EndTime = DateTime.Now;
        _currentSession = null!;
        CurrentSessionMessages = new List<ChatMessage>();
    }

    public ChatSession? GetSessionById(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return null!;
        return Sessions.FirstOrDefault(s => s.SessionId == sessionId);
    }

    public List<ChatMessage> GetMessagesBySession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return new List<ChatMessage>();

        var session = GetSessionById(sessionId);
        return session?.Messages ?? new List<ChatMessage>();
    }

    public List<ChatMessage> GetAllMessages()
    {
        var allMessages = new List<ChatMessage>();
        foreach (var session in Sessions)
        {
            allMessages.AddRange(session.Messages);
        }
        return allMessages;
    }

    public void ClearHistory()
    {
        Sessions.Clear();
        CurrentSessionMessages.Clear();
        _currentSession = null!;
    }

    public void TrimOldMessages()
    {
        if (CurrentSessionMessages.Count <= MaxMessagesPerSession) return;

        int removeCount = CurrentSessionMessages.Count - MaxMessagesPerSession;
        CurrentSessionMessages.RemoveRange(0, removeCount);
    }

    public void SaveToFile()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        try
        {
            string json = JsonSerializer.Serialize(Sessions, options);

            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to save chat history: {ex.Message}");
        }
    }

    public void LoadFromFile()
    {
        if (!Godot.FileAccess.FileExists(SavePath))
        {
            return;
        }

        try
        {
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null) return;

            string json = file.GetAsText();
            var sessions = JsonSerializer.Deserialize<List<ChatSession>>(json);

            if (sessions != null)
            {
                Sessions = sessions;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to load chat history: {ex.Message}");
        }
    }
}
