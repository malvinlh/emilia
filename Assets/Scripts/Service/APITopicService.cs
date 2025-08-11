// APITopicService.cs
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APITopicService : MonoBehaviour
{
    public static APITopicService Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string baseUrl    = "http://localhost:1204";
    [SerializeField] private string endpoint   = "/topic";
    [SerializeField] private int    timeoutSec = 15;

    [Serializable] private class TopicResponse { public string response; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    /// <summary>
    /// Panggil POST /topic (x-www-form-urlencoded) dengan field: user, bot.
    /// </summary>
    public IEnumerator GetTopic(
        string userText,
        string botText,
        Action<string> onSuccess,
        Action<string> onError = null)
    {
        string enc(string s) => Uri.EscapeDataString(s ?? "");

        var payload = $"user={enc(userText)}&bot={enc(botText ?? "")}";
        var bytes   = Encoding.UTF8.GetBytes(payload);

        var url = $"{baseUrl}{endpoint}";
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler   = new UploadHandlerRaw(bytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout         = timeoutSec;
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                onError?.Invoke($"HTTP {(int)req.responseCode}: {req.error}");
                yield break;
            }

            try
            {
                var parsed = JsonUtility.FromJson<TopicResponse>(req.downloadHandler.text);
                var topic  = parsed?.response?.Trim();
                if (string.IsNullOrEmpty(topic))
                    onError?.Invoke("Topic kosong / tidak valid.");
                else
                    onSuccess?.Invoke(topic);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Gagal parse JSON: {e.Message}");
            }
        }
    }
}




