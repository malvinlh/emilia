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
            onResult(first?.Text ?? "");
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

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

    #endregion

    #region Commands

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
                StartedAt = DateTime.UtcNow
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
            var msg = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Sender = sender,
                Text = content,
                SentAt = DateTime.UtcNow
            };
            _db.Insert(msg);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
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