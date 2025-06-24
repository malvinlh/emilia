// ChatManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    const int SNIPPET_MAX_LENGTH = 20;  // panjang maksimal sebelum "..."

    [Header("Prefabs & UI")]
    public GameObject userBubblePrefab;
    public GameObject aiBubblePrefab;
    public GameObject historyButtonPrefab;
    public Transform chatContentParent;
    public Transform chatHistoryParent;
    public TMP_InputField inputField;
    public Button newChatButton;
    public Button sendButton;

    [HideInInspector]
    public string CurrentUserId;

    private string currentConversationId;
    private bool isAwaitingResponse = false;
    private bool isNewConversation   = false;

    void Awake()
    {
        CurrentUserId = PlayerPrefs.GetString("Nickname", "");
    }

    void Start()
    {
        newChatButton.onClick.AddListener(OnNewChatClicked);
        sendButton.onClick.AddListener(OnSendClicked);

        StartCoroutine(
            ServiceManager.Instance.ChatService.FetchConversations(
                CurrentUserId,
                PopulateHistoryButtons,
                err => Debug.LogError("Fetch conv IDs failed: " + err)
            )
        );
    }

    private void PopulateHistoryButtons(string[] convIds)
    {
        // clear existing
        for (int i = chatHistoryParent.childCount - 1; i >= 0; i--)
            Destroy(chatHistoryParent.GetChild(i).gameObject);

        // instantiate one button per convId
        for (int i = 0; i < convIds.Length; i++)
        {
            string convId = convIds[i];
            var go = Instantiate(historyButtonPrefab, chatHistoryParent);
            var hb = go.GetComponent<HistoryButton>();
            hb.SetConversationId(convId);

            // fetch snippet untuk pesan pertama
            StartCoroutine(
                ServiceManager.Instance.ChatService.FetchFirstMessage(
                    convId,
                    firstMsg =>
                    {
                        // potong kalau terlalu panjang
                        string snippet = string.IsNullOrEmpty(firstMsg)
                            ? $"Chat {i+1}"
                            : (firstMsg.Length > SNIPPET_MAX_LENGTH
                                ? firstMsg.Substring(0, SNIPPET_MAX_LENGTH) + "..."
                                : firstMsg);
                        hb.SetLabel(snippet);
                    },
                    err =>
                    {
                        Debug.LogError("Fetch first msg failed: " + err);
                        hb.SetLabel($"Chat {i+1}");
                    }
                )
            );
        }

        isNewConversation = false;
    }

    public void OnHistoryClicked(string conversationId)
    {
        currentConversationId = conversationId;
        isNewConversation     = false;
        ClearChat();

        StartCoroutine(
            ServiceManager.Instance.ChatService.FetchConversationWithMessages(
                conversationId,
                CurrentUserId,
                msgs =>
                {
                    foreach (var msg in msgs)
                        CreateBubble(msg.message, msg.sender == CurrentUserId);
                },
                err => Debug.LogError("Fetch history error: " + err)
            )
        );
    }

    private void OnNewChatClicked()
    {
        ClearChat();
        currentConversationId = null;
        isNewConversation     = true;
    }

    private void OnSendClicked()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || isAwaitingResponse)
            return;

        inputField.text = "";
        CreateBubble(text, true);

        Debug.Log($"[ChatManager] OnSendClicked: conversationId = '{currentConversationId}', user = '{CurrentUserId}', message = '{text}'");

        if (isNewConversation)
        {
            int idx = chatHistoryParent.childCount + 1;
            currentConversationId = $"cv{idx:00}";
            isNewConversation = false;

            Debug.Log($"[ChatManager] New conversation generated: '{currentConversationId}'");

            var go = Instantiate(historyButtonPrefab, chatHistoryParent);
            var hb = go.GetComponent<HistoryButton>();
            hb.SetConversationId(currentConversationId);

            // langsung tampilkan snippet pesan pertama yang baru saja dikirim
            string snippet = text.Length > SNIPPET_MAX_LENGTH
                ? text.Substring(0, SNIPPET_MAX_LENGTH) + "..."
                : text;
            hb.SetLabel(snippet);

            // upsert conversation sebelum insert message
            StartCoroutine(
                ServiceManager.Instance.ChatService.UpsertConversation(
                    currentConversationId,
                    CurrentUserId,
                    onSuccess: () => StartCoroutine(SendUserMessage(text)),
                    onError: err => Debug.LogError("Upsert convo failed: " + err)
                )
            );
        }
        else
        {
            StartCoroutine(SendUserMessage(text));
        }
    }

    private IEnumerator SendUserMessage(string text)
    {
        yield return ServiceManager.Instance.ChatService.InsertMessage(
            currentConversationId,
            CurrentUserId,
            text,
            onSuccess: () => StartCoroutine(HandleAITurn(text)),
            onError: err => Debug.LogError("Insert message failed: " + err)
        );
    }

    private void CreateBubble(string msg, bool isUser)
    {
        var prefab = isUser ? userBubblePrefab : aiBubblePrefab;
        var go = Instantiate(prefab, chatContentParent);
        go.GetComponent<ChatBubbleController>()?.SetText(msg);
    }

    private void ClearChat()
    {
        for (int i = chatContentParent.childCount - 1; i >= 0; i--)
            Destroy(chatContentParent.GetChild(i).gameObject);
    }

    private IEnumerator HandleAITurn(string userMessage)
    {
        isAwaitingResponse = true;
        var typingGO = Instantiate(aiBubblePrefab, chatContentParent);
        var anim     = StartCoroutine(AnimateTyping(typingGO.GetComponent<ChatBubbleController>()));

        yield return OllamaService.SendPrompt(userMessage, response =>
        {
            StopCoroutine(anim);
            Destroy(typingGO);
            CreateBubble(response, false);

            StartCoroutine(
                ServiceManager.Instance.ChatService.InsertMessage(
                    currentConversationId,
                    "Bot",
                    response,
                    onSuccess: () => Debug.Log("✅ AI message saved"),
                    onError: err => Debug.LogError("Save AI message failed: " + err)
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