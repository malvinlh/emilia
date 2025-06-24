using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TestQuery : MonoBehaviour
{
    // Your Supabase project URL + REST path
    private const string BASE_URL = "https://ocpnfpnkxgkmplhrnbtt.supabase.co/rest/v1/journals";
    // Use your anon (public) API key for client-side reads
    private const string API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9jcG5mcG5reGdrbXBsaHJuYnR0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTA0NDU4NTEsImV4cCI6MjA2NjAyMTg1MX0.VvdGQrAXD14-XfAQcmYqfrK9Y4Gux72AAPPoJMhPx7s";
    // (Optional) Service role key—do NOT ship this in builds!
    private const string SECRET_API_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9jcG5mcG5reGdrbXBsaHJuYnR0Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc1MDQ0NTg1MSwiZXhwIjoyMDY2MDIxODUxfQ.Kt99P-9XqoqFfHQiICY_IRzoXlQaRVCQ5uMJQuIrNnI";

    void Start()
    {
        // Kick off the fetch as soon as this GameObject is enabled
        StartCoroutine(FetchJournals());
    }

    IEnumerator FetchJournals()
    {
        // Build the URL with a simple select all
        string url = $"{BASE_URL}?select=*";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            // Required by Supabase/PostgREST for key-auth
            req.SetRequestHeader("apikey", API_KEY);
            req.SetRequestHeader("Authorization", $"Bearer {API_KEY}");
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Journals fetched successfully:\n" + req.downloadHandler.text);
                // You can now parse req.downloadHandler.text as JSON
            }
            else
            {
                Debug.LogError($"❌ Error fetching journals: {req.error} (HTTP {req.responseCode})");
            }
        }
    }
}
