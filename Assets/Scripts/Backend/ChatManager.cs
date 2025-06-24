using UnityEngine;
using TMPro;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    [Header("Prefabs & UI References")]
    public GameObject userBubblePrefab;
    public GameObject aiBubblePrefab;
    public GameObject historyButtonPrefab;  // Prefab yang sudah attach HistoryButton.cs
    public Transform chatContentParent;     // Container bubble
    public Transform chatHistoryParent;     // Sidebar container
    public TMP_InputField inputField;

    [HideInInspector]
    public string CurrentUserId;

    private string currentConversationId;
    private bool isAwaitingResponse = false;

    void Awake()
    {
        // Baca nickname dari PlayerPrefs
        CurrentUserId = PlayerPrefs.GetString("Nickname", "");
    }

    void Start()
    {
        // 1) Ambil semua ID percakapan user
        StartCoroutine(
            ServiceManager.Instance.ChatService.FetchConversations(
                CurrentUserId,
                ids => PopulateHistoryButtons(ids),
                err => Debug.LogError("Fetch conv IDs failed: " + err)
            )
        );
    }

    /// <summary>
    /// Buat satu tombol per ID convIds[i].
    /// </summary>
    private void PopulateHistoryButtons(string[] convIds)
    {
        // bersihkan dulu child lama
        for (int i = chatHistoryParent.childCount - 1; i >= 0; i--)
            Destroy(chatHistoryParent.GetChild(i).gameObject);

        // instantiate tombol baru
        for (int i = 0; i < convIds.Length; i++)
        {
            string id = convIds[i];            // "cv01", "cv02", …
            var go = Instantiate(historyButtonPrefab, chatHistoryParent);

            var hb = go.GetComponent<HistoryButton>();
            hb.SetConversationId(id);          // langsung set ID
            hb.SetLabel($"Chat {i+1}");        // optional: label tombol
        }
    }

    /// <summary>
    /// Dipanggil HistoryButton saat di-klik
    /// </summary>
    public void OnHistoryClicked(string conversationId)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
            return;

        currentConversationId = conversationId;
        ClearChat();

        // 2) Fetch pesan
        StartCoroutine(
            ServiceManager.Instance.ChatService.FetchConversationWithMessages(
                conversationId,
                CurrentUserId,
                messages =>
                {
                    foreach (var msg in messages)
                    {
                        bool isUser = msg.sender == CurrentUserId;
                        CreateBubble(msg.message, isUser);
                    }
                },
                err => Debug.LogError("Fetch history error: " + err)
            )
        );
    }

    /// <summary>
    /// Dipanggil saat user kirim pesan baru
    /// </summary>
    public void OnSendClicked()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text)
            || isAwaitingResponse
            || string.IsNullOrEmpty(currentConversationId))
            return;

        inputField.text = "";
        CreateBubble(text, true);

        // 3) Simpan pesan user
        StartCoroutine(
            ServiceManager.Instance.ChatService.InsertMessage(
                currentConversationId,
                CurrentUserId,
                text,
                onSuccess: () => StartCoroutine(HandleAITurn(text)),
                onError: err => Debug.LogError("Insert user msg failed: " + err)
            )
        );
    }

    private void CreateBubble(string message, bool isUser)
    {
        var prefab = isUser ? userBubblePrefab : aiBubblePrefab;
        var go     = Instantiate(prefab, chatContentParent);
        go.GetComponent<ChatBubbleController>()?.SetText(message);
    }

    private void ClearChat()
    {
        for (int i = chatContentParent.childCount - 1; i >= 0; i--)
            Destroy(chatContentParent.GetChild(i).gameObject);
    }

    private IEnumerator HandleAITurn(string userMessage)
    {
        isAwaitingResponse = true;

        // 4) Bubble “typing…”
        var typingGO   = Instantiate(aiBubblePrefab, chatContentParent);
        var typingCtrl = typingGO.GetComponent<ChatBubbleController>();
        var anim       = StartCoroutine(AnimateTyping(typingCtrl));

        // 5) Kirim ke LLM
        yield return OllamaService.SendPrompt(userMessage, response =>
        {
            StopCoroutine(anim);
            Destroy(typingGO);

            CreateBubble(response, false);

            // 6) Simpan pesan AI
            StartCoroutine(
                ServiceManager.Instance.ChatService.InsertMessage(
                    currentConversationId,
                    "Bot",
                    response,
                    onSuccess: () => Debug.Log("✅ AI message saved"),
                    onError: err => Debug.LogError("Gagal simpan AI message: " + err)
                )
            );

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
