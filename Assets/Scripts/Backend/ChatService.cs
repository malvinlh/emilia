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

/// <summary>
/// Turunan dari SupabaseHttpClient yang punya SendRequest().
/// </summary>
public class ChatService : SupabaseHttpClient
{
    const string ConvTable = "conversations";
    const string MsgTable  = "messages";

    /// <summary>
    /// 1) GET semua ID percakapan milik user (cv01, cv02, …)
    /// 2) onSuccess diberikan array string[] convIds
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
                path:      path,
                method:    "GET",
                bodyJson:  null,
                onSuccess: resp =>
                {
                    // bungkus JSON array menjadi objek untuk JsonUtility
                    var wrapped = $"{{\"items\":{resp}}}";
                    var list    = JsonUtility.FromJson<ConversationIdListWrapper>(wrapped);

                    if (list.items != null)
                    {
                        string[] ids = new string[list.items.Length];
                        for (int i = 0; i < ids.Length; i++)
                            ids[i] = list.items[i].id;   // pasti "cv01", "cv02", …
                        onSuccess(ids);
                    }
                    else
                    {
                        onSuccess(Array.Empty<string>());
                    }
                },
                onError: err => onError?.Invoke(err)
            )
        );
    }

    /// <summary>
    /// GET satu percakapan beserta semua pesannya
    /// </summary>
    public IEnumerator FetchConversationWithMessages(
        string conversationId,
        string userId,
        Action<Message[]> onResult,
        Action<string> onError = null
    )
    {
        string select = $"{ConvTable}?select=*,messages(*)" +
                        $"&id=eq.{conversationId}&user_id=eq.{userId}";
        yield return StartCoroutine(
            SendRequest(
                path:      select,
                method:    "GET",
                bodyJson:  null,
                onSuccess: resp =>
                {
                    var wrapped = $"{{\"items\":{resp}}}";
                    var w       = JsonUtility.FromJson<ConversationListWrapper>(wrapped);
                    if (w.items != null && w.items.Length > 0)
                        onResult(w.items[0].messages);
                    else
                        onResult(Array.Empty<Message>());
                },
                onError: err => onError?.Invoke(err)
            )
        );
    }

    /// <summary>
    /// INSERT satu pesan
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

        var reqObj = new InsertRequest {
            id              = Guid.NewGuid().ToString(),
            conversation_id = conversationId,
            sender          = sender,
            message         = content,
            sent_at         = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
        };
        string json = "[" + JsonUtility.ToJson(reqObj) + "]";

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
