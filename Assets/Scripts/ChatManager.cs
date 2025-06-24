using UnityEngine;
using TMPro;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject userBubblePrefab;
    public GameObject aiBubblePrefab;

    [Header("UI References")]
    public Transform chatContentParent;
    public TMP_InputField inputField;

    [Header("Services")]
    public SupabaseClient supabaseClient;

    // <-- tambahkan ini:
    private string currentConversationId;

    public string CurrentUserId = "Alice";
    private bool isAwaitingResponse = false;

    public void OnHistoryClicked(string conversationId)
    {
        // simpan convo id yang aktif
        currentConversationId = conversationId;
        ClearChat();
        StartCoroutine(supabaseClient.FetchConversationWithMessages(conversationId, CurrentUserId));
    }

    public void OnSendClicked()
    {
        // 1) Ambil teks dari input field
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || isAwaitingResponse || string.IsNullOrEmpty(currentConversationId))
            return;

        // 2) Kosongkan input field **setelah** ambil teks
        inputField.text = "";

        // 3) Render bubble di UI
        CreateBubble(text, true);

        // 4) Insert ke Supabase pakai variabel 'text'
        StartCoroutine(supabaseClient.InsertMessage(
            currentConversationId,
            CurrentUserId,
            text,
            onSuccess: () => {
                StartCoroutine(HandleAITurn(text));
            },
            onError: err => {
                Debug.LogError("Gagal insert chat: " + err);
            }
        ));
    }

    public void CreateBubble(string message, bool isUser)
    {
        var prefab = isUser ? userBubblePrefab : aiBubblePrefab;
        var go = Instantiate(prefab, chatContentParent);
        go.GetComponent<ChatBubbleController>()?.SetText(message);
    }

    public void ClearChat()
    {
        foreach (Transform t in chatContentParent)
            Destroy(t.gameObject);
    }

    private IEnumerator HandleAITurn(string userMessage)
    {
        isAwaitingResponse = true;

        // 1) Bubble “typing…”
        var typingGO   = Instantiate(aiBubblePrefab, chatContentParent);
        var typingCtrl = typingGO.GetComponent<ChatBubbleController>();
        var anim = StartCoroutine(AnimateTyping(typingCtrl));

        // 2) Kirim prompt ke Ollama
        yield return OllamaService.SendPrompt(userMessage, response =>
        {
            // 3) Stop animasi & remove bubble typing
            StopCoroutine(anim);
            Destroy(typingGO);

            // 4) Tampilkan bubble AI
            CreateBubble(response, false);

            // 5) Insert AI message ke Supabase
            StartCoroutine(supabaseClient.InsertMessage(
                currentConversationId,     // id convo yang sedang aktif
                "Bot",                      // atau nama sender yang kamu inginkan
                response,
                onSuccess: () => {
                    Debug.Log("✅ AI message saved");
                },
                onError: err => {
                    Debug.LogError("Gagal simpan AI message: " + err);
                }
            ));

            isAwaitingResponse = false;
        });
    }

    private IEnumerator AnimateTyping(ChatBubbleController ctrl)
    {
        var dots = new[] { "", ".", ". .", ". . ." };
        int i = 0;
        while (true)
        {
            ctrl.SetText(dots[i]);
            i = (i + 1) % dots.Length;
            yield return new WaitForSeconds(0.5f);
        }
    }
}
