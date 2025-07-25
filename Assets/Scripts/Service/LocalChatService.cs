// LocalChatService.cs
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using SQLite;
using EMILIA.Data;

public class LocalChatService : MonoBehaviour
{
    private SQLiteConnection _db;

    void Awake()
    {
        _db = DatabaseManager.Instance.DB;
        // ensure foreign-key constraints (optional)
        _db.Execute("PRAGMA foreign_keys = ON;");
    }

    /// <summary>
    /// GET all conversation IDs for this user, ordered by started_at DESC
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
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// INSERT a new conversation row
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
            var convo = new Conversation {
                Id        = conversationId,
                UserId    = userId,
                StartedAt = DateTime.UtcNow
            };
            _db.Insert(convo);
            onSuccess?.Invoke();
        }
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// INSERT a new message row
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
            var msg = new Message {
                Id             = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Sender         = sender,
                Text           = content,
                SentAt         = DateTime.UtcNow
            };
            _db.Insert(msg);
            onSuccess?.Invoke();
        }
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// GET the very first (oldest) message text for a conversation
    /// </summary>
    public IEnumerator FetchFirstMessage(
        string conversationId,
        Action<string> onResult,
        Action<string> onError = null
    )
    {
        try
        {
            var first = _db.Table<Message>()
                           .Where(m => m.ConversationId == conversationId)
                           .OrderBy(m => m.SentAt)
                           .FirstOrDefault();
            onResult(first != null ? first.Text : "");
        }
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// GET all messages for a conversation, ordered by sent_at ASC
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
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// DELETE messages for a list of conversation IDs
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
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// DELETE all conversations for a user
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
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// DELETE messages then conversations for a user
    /// </summary>
    public IEnumerator DeleteAllChats(
        string userId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        // 1) fetch IDs
        yield return FetchUserConversations(userId, convIds => {
            // 2) delete messages
            StartCoroutine(DeleteMessages(convIds, () => {
                // 3) delete conv rows
                StartCoroutine(DeleteConversations(userId, onSuccess, onError));
            }, onError));
        }, onError);
    }

    /// <summary>
    /// DELETE all messages for a single conversation
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
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }

    /// <summary>
    /// DELETE one conversation row
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
        catch (Exception ex) { onError?.Invoke(ex.Message); }
        yield break;
    }
}