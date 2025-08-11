// APISummaryService.cs
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class APISummaryService : MonoBehaviour
{
    public static APISummaryService Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string baseUrl    = "http://localhost:1204";
    [SerializeField] private string endpoint   = "/summary";
    [SerializeField] private int    timeoutSec = 20;

    [Serializable] private class SummaryResponse { public string response; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    /// <summary>
    /// POST /summary (application/x-www-form-urlencoded)
    /// Body: conv_id=<conversationId>
    /// </summary>
    public IEnumerator RequestSummary(
        string conversationId,
        Action<string> onSuccess,
        Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            onError?.Invoke("conversationId kosong.");
            yield break;
        }

        string enc(string s) => Uri.EscapeDataString(s ?? "");
        var payload = $"conv_id={enc(conversationId)}";
        var bytes   = Encoding.UTF8.GetBytes(payload);

        using (var req = new UnityWebRequest($"{baseUrl}{endpoint}", UnityWebRequest.kHttpVerbPOST))
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
                var parsed = JsonUtility.FromJson<SummaryResponse>(req.downloadHandler.text);
                var text   = parsed?.response?.Trim();
                if (string.IsNullOrEmpty(text))
                    onError?.Invoke("Summary kosong / invalid.");
                else
                    onSuccess?.Invoke(text);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Gagal parse JSON: {e.Message}");
            }
        }
    }
}