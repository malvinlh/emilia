// ChatService.cs
using UnityEngine;
using System;
using System.Collections;

[Serializable]
public class Message
{
    public string id;
    public string conversation_id;
    public string sender;
    public string message;
    public string sent_at;
}

[Serializable]
public class Conversation
{
    public string id;
    public string user_id;
    public string started_at;
    public string ended_at;
    public Message[] messages;
}

[Serializable]
public class ConversationListWrapper
{
    public Conversation[] items;
}

[Serializable]
public class ConversationIdWrapper
{
    public string id;
}

[Serializable]
public class ConversationIdListWrapper
{
    public ConversationIdWrapper[] items;
}

[Serializable]
public class InsertRequest
{
    public string id;
    public string conversation_id;
    public string sender;
    public string message;
    public string sent_at;
}

[Serializable]
public class ConversationUpsert
{
    public string id;
    public string user_id;
    public string started_at;
}

// — THESE TWO WERE MISSING —
[Serializable]
public class FirstMessageWrapper
{
    public string message;
}

[Serializable]
public class FirstMessageListWrapper
{
    public FirstMessageWrapper[] items;
}

/// <summary>
/// Inherits SupabaseHttpClient (with SendRequest).
/// Contains all conversation + message endpoints.
/// </summary>
public class ChatService : SupabaseHttpClient
{
    const string ConvTable = "conversations";
    const string MsgTable  = "messages";

    /// <summary>
    /// GET all conversation IDs for this user.
    /// </summary>
    public IEnumerator FetchConversations(
        string userId,
        Action<string[]> onSuccess,
        Action<string> onError = null
    )
    {
        string path = $"{ConvTable}?select=id&user_id=eq.{userId}";
        yield return StartCoroutine(
            SendRequest(
                path, "GET", null,
                resp =>
                {
                    var wrapped = $"{{\"items\":{resp}}}";
                    var list    = JsonUtility.FromJson<ConversationIdListWrapper>(wrapped);
                    if (list.items != null)
                    {
                        var ids = new string[list.items.Length];
                        for (int i = 0; i < list.items.Length; i++)
                            ids[i] = list.items[i].id;
                        onSuccess(ids);
                    }
                    else
                    {
                        onSuccess(Array.Empty<string>());
                    }
                },
                err => onError?.Invoke(err)
            )
        );
    }

    /// <summary>
    /// GET the first (oldest) message for a conversation—used for snippet.
    /// </summary>
    public IEnumerator FetchFirstMessage(
        string conversationId,
        Action<string> onResult,
        Action<string> onError = null
    )
    {
        string path =
            $"{MsgTable}?select=message&conversation_id=eq.{conversationId}" +
            $"&order=sent_at.asc&limit=1";
        yield return StartCoroutine(
            SendRequest(
                path, "GET", null,
                resp =>
                {
                    var wrapped = $"{{\"items\":{resp}}}";
                    var list    = JsonUtility.FromJson<FirstMessageListWrapper>(wrapped);
                    if (list.items != null && list.items.Length > 0)
                        onResult(list.items[0].message);
                    else
                        onResult(string.Empty);
                },
                err => onError?.Invoke(err)
            )
        );
    }

    /// <summary>
    /// GET one conversation + all its messages.
    /// </summary>
    public IEnumerator FetchConversationWithMessages(
        string conversationId,
        string userId,
        Action<Message[]> onResult,
        Action<string> onError = null
    )
    {
        string select =
            $"{ConvTable}?select=*,messages(*)" +
            $"&id=eq.{conversationId}&user_id=eq.{userId}";
        yield return StartCoroutine(
            SendRequest(
                select, "GET", null,
                resp =>
                {
                    var wrapped = $"{{\"items\":{resp}}}";
                    var w       = JsonUtility.FromJson<ConversationListWrapper>(wrapped);
                    if (w.items != null && w.items.Length > 0)
                        onResult(w.items[0].messages);
                    else
                        onResult(Array.Empty<Message>());
                },
                err => onError?.Invoke(err)
            )
        );
    }

    /// <summary>
    /// Upsert conversation before inserting messages.
    /// </summary>
    public IEnumerator UpsertConversation(
        string conversationId,
        string userId,
        Action onSuccess,
        Action<string> onError
    )
    {
        Debug.Log($"[ChatService] UpsertConversation: id = '{conversationId}', userId = '{userId}'");

        var payload = new ConversationUpsert
        {
            id         = conversationId,
            user_id    = userId,
            started_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
        };
        string json = "[" + JsonUtility.ToJson(payload) + "]";

        string path = $"{ConvTable}?on_conflict=id";
        yield return StartCoroutine(
            SendRequest(
                path:         path,
                method:       "POST",
                bodyJson:     json,
                onSuccess:    _ => onSuccess?.Invoke(),
                onError:      err => onError?.Invoke(err),
                preferHeader: "resolution=merge-duplicates"
            )
        );
    }

    /// <summary>
    /// INSERT a new message.
    /// </summary>
    public IEnumerator InsertMessage(
        string conversationId,
        string sender,
        string content,
        Action onSuccess = null,
        Action<string> onError = null
    )
    {
        if (string.IsNullOrEmpty(content))
        {
            onError?.Invoke("Empty content");
            yield break;
        }

        Debug.Log($"[ChatService] InsertMessage: conversationId = '{conversationId}', sender = '{sender}', content = '{content}'");

        var req = new InsertRequest
        {
            id              = Guid.NewGuid().ToString(),
            conversation_id = conversationId,
            sender          = sender,
            message         = content,
            sent_at         = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
        };
        string json = "[" + JsonUtility.ToJson(req) + "]";

        yield return StartCoroutine(
            SendRequest(
                path:         MsgTable,
                method:       "POST",
                bodyJson:     json,
                onSuccess:    _ => onSuccess?.Invoke(),
                onError:      err => onError?.Invoke(err),
                preferHeader: "return=representation"
            )
        );
    }
}
