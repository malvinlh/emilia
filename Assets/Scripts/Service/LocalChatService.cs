using System;
using System.Collections;
using System.Linq;
using SQLite;
using UnityEngine;
using EMILIA.Data;

public class LocalChatService : MonoBehaviour
{
    #region Dependencies

    private SQLiteConnection _db;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _db = DatabaseManager.Instance.DB;
        _db.Execute("PRAGMA foreign_keys = ON;");

        // === MIGRASI: tambah kolom title jika belum ada ===
        EnsureTitleColumn();
    }

    private void EnsureTitleColumn()
    {
        try
        {
            // Akan throw "duplicate column name: title" jika sudah ada → aman diabaikan
            _db.Execute("ALTER TABLE conversations ADD COLUMN title TEXT;");
        }
        catch (Exception ex)
        {
            // Abaikan kalau kolom sudah ada, log jika error lain
            if (!ex.Message.ToLower().Contains("duplicate column name"))
                Debug.LogWarning($"[LocalChatService] EnsureTitleColumn warning: {ex.Message}");
        }
    }

    #endregion

    #region Queries

    /// <summary>
    /// Retrieves all conversation IDs for a user, ordered by start time descending.
    /// </summary>
    public IEnumerator FetchUserConversations(
        string userId,
        Action<string[]> onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            var ids = _db.Table<Conversation>()
                         .Where(c => c.UserId == userId)
                         .OrderByDescending(c => c.StartedAt)
                         .Select(c => c.Id)
                         .ToArray();
            onSuccess(ids);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// Retrieves the very first (oldest) message text for a conversation.
    /// </summary>
    // public IEnumerator FetchFirstMessage(
    //     string conversationId,
    //     Action<string> onResult,
    //     Action<string> onError = null
    // )
    // {
    //     try
    //     {
    //         var first = _db.Table<Message>()
    //                        .Where(m => m.ConversationId == conversationId)
    //                        .OrderBy(m => m.SentAt)
    //                        .FirstOrDefault();
    //         onResult(first?.Text ?? "");
    //     }
    //     catch (Exception ex)
    //     {
    //         onError?.Invoke(ex.Message);
    //     }
    //     yield break;
    // }

    /// <summary>
    /// Retrieves all messages for a conversation, ordered by sent time ascending.
    /// </summary>
    public IEnumerator FetchConversationWithMessages(
        string conversationId,
        string userId,
        Action<Message[]> onResult,
        Action<string> onError = null
    )
    {
        try
        {
            var msgs = _db.Table<Message>()
                          .Where(m => m.ConversationId == conversationId)
                          .OrderBy(m => m.SentAt)
                          .ToArray();
            onResult(msgs);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// NEW: Ambil title yang tersimpan untuk percakapan.
    /// </summary>
    public string GetConversationTitle(string conversationId)
    {
        try
        {
            var c = _db.Find<Conversation>(conversationId);
            return c?.Title;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocalChatService] GetConversationTitle: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Commands

    // LocalChatService.cs (tambahkan di #region Commands)
    public IEnumerator InsertSummary(
        string conversationId,
        string summaryText,
        Action onSuccess = null,
        Action<string> onError = null
    )
    {
        Debug.LogError($"[LocalChatService] InsertSummary: {conversationId}");
        try
        {
            var row = new Summary
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                SummaryText = summaryText ?? "",
                CreatedAt = DateTime.UtcNow
            };

            // INSERT
            _db.Insert(row);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// Inserts a new conversation record.
    /// </summary>
    public IEnumerator CreateConversation(
        string conversationId,
        string userId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            var convo = new Conversation
            {
                Id = conversationId,
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                Title = null
            };
            _db.Insert(convo);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// Inserts a new message record.
    /// </summary>
    public IEnumerator InsertMessage(
        string conversationId,
        string sender,
        string content,
        Action onSuccess = null,
        Action<string> onError = null
    )
    {
        try
        {
            var now = DateTime.UtcNow;

            var msg = new Message
            {
                Id             = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Sender         = sender,
                Text           = content,
                SentAt         = now
            };
            _db.Insert(msg);

            // dipakai sebagai "last activity"
            _db.Execute(
                "UPDATE conversations SET started_at = ? WHERE id = ?",
                now,
                conversationId
            );

            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// NEW: Update title untuk conversation (sinkron, ringan).
    /// </summary>
    public void UpdateConversationTitle(string conversationId, string title)
    {
        try
        {
            _db.Execute("UPDATE conversations SET title = ? WHERE id = ?", title, conversationId);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LocalChatService] UpdateConversationTitle: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes all messages for the given conversation IDs.
    /// </summary>
    public IEnumerator DeleteMessages(
        string[] convIds,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            foreach (var id in convIds)
                _db.Execute("DELETE FROM messages WHERE conversation_id = ?", id);

            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// Deletes all messages for a single conversation.
    /// </summary>
    public IEnumerator DeleteMessagesForConversation(
        string conversationId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            _db.Execute("DELETE FROM messages WHERE conversation_id = ?", conversationId);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// Deletes all conversations for a user.
    /// </summary>
    public IEnumerator DeleteConversations(
        string userId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            _db.Execute("DELETE FROM conversations WHERE user_id = ?", userId);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// Deletes a single conversation record.
    /// </summary>
    public IEnumerator DeleteConversation(
        string conversationId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            _db.Execute("DELETE FROM conversations WHERE id = ?", conversationId);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    #endregion

    #region Composite Operations

    /// <summary>
    /// Deletes all messages and then all conversations for a user.
    /// </summary>
    public IEnumerator DeleteAllChats(
        string userId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        yield return FetchUserConversations(
            userId,
            convIds =>
            {
                StartCoroutine(
                    DeleteMessages(
                        convIds,
                        () => StartCoroutine(DeleteConversations(userId, onSuccess, onError)),
                        onError
                    )
                );
            },
            onError
        );
    }

    #endregion
}