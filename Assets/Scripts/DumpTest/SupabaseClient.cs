using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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
public class InsertRequest
{
    public string id;
    public string conversation_id;
    public string sender;
    public string message;
    public string sent_at;
}

public class SupabaseClient : MonoBehaviour
{
    private const string supabaseUrl = "https://ocpnfpnkxgkmplhrnbtt.supabase.co";
    private const string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9jcG5mcG5reGdrbXBsaHJuYnR0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTA0NDU4NTEsImV4cCI6MjA2NjAyMTg1MX0.VvdGQrAXD14-XfAQcmYqfrK9Y4Gux72AAPPoJMhPx7s";

    [Header("Bindings")]
    public ChatManager chatManager;

    /// <summary>
    /// Fetch semua messages untuk satu conversation, lalu tampilkan via ChatManager.
    /// </summary>
    public IEnumerator FetchConversationWithMessages(string conversationId, string userId)
    {
        var endpoint = $"{supabaseUrl}/rest/v1/conversations" +
                       $"?id=eq.{Uri.EscapeDataString(conversationId)}" +
                       $"&user_id=eq.{Uri.EscapeDataString(userId)}" +
                       "&select=messages(*)";

        Debug.Log($"[Supabase] GET {endpoint}");

        using var req = UnityWebRequest.Get(endpoint);
        req.SetRequestHeader("apikey", supabaseKey);
        req.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        req.SetRequestHeader("Accept", "application/json");

        yield return req.SendWebRequest();

        Debug.Log($"[Supabase] Status: {req.responseCode}  Body: {req.downloadHandler.text}");
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ Supabase fetch error: {req.error}");
            yield break;
        }

        string wrapped = $"{{\"items\":{req.downloadHandler.text}}}";
        var wrapper = JsonUtility.FromJson<ConversationListWrapper>(wrapped);
        if (wrapper.items == null || wrapper.items.Length == 0)
        {
            Debug.LogWarning("🚧 Conversation not found or empty.");
            yield break;
        }

        foreach (var msg in wrapper.items[0].messages)
        {
            bool isUser = string.Equals(msg.sender, userId, StringComparison.OrdinalIgnoreCase);
            chatManager.CreateBubble(msg.message, isUser);
        }
    }

    /// <summary>
    /// Insert single message ke table `messages`, termasuk id dan timestamp.
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
            Debug.LogWarning("Cannot insert empty message.");
            yield break;
        }

        var endpoint = $"{supabaseUrl}/rest/v1/messages";
        Debug.Log($"[Supabase] POST {endpoint}");

        // Generate id and sent_at timestamp
        string newId = Guid.NewGuid().ToString();
        string sentAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

        // Build request object
        var reqObj = new InsertRequest
        {
            id = newId,
            conversation_id = conversationId,
            sender = sender,
            message = content,
            sent_at = sentAt
        };
        string body = JsonUtility.ToJson(reqObj);
        Debug.Log($"[Supabase] Request Body: {body}");

        using var req = new UnityWebRequest(endpoint, "POST");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("apikey", supabaseKey);
        req.SetRequestHeader("Authorization", $"Bearer {supabaseKey}");
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Prefer", "return=representation");

        yield return req.SendWebRequest();

        Debug.Log($"[Supabase] Status: {req.responseCode}  Response: {req.downloadHandler.text}");
        if (req.result != UnityWebRequest.Result.Success || req.responseCode >= 400)
        {
            Debug.LogError($"❌ InsertMessage failed: {req.downloadHandler.text}");
            onError?.Invoke(req.downloadHandler.text);
        }
        else
        {
            Debug.Log("✅ Message inserted successfully");
            onSuccess?.Invoke();
        }
    }
}
