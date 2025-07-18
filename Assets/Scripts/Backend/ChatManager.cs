// ChatManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ChatManager : MonoBehaviour
{
    [Header("Prefabs & UI References")]
    public GameObject     userBubblePrefab;
    public GameObject     aiBubblePrefab;
    public GameObject     historyButtonPrefab;
    public Transform      chatContentParent;
    public Transform      chatHistoryParent;
    public TMP_InputField inputField;
    public Button         newChatButton;
    public Button         sendButton;

    [Header("Delete Confirmation UI")]
    public GameObject    deleteChatSetting;   // panel konfirmasi
    public Button        deleteYesButton;
    public Button        deleteNoButton;

    private string       pendingDeleteId;

    [HideInInspector]
    public string         CurrentUserId;

    private string        currentConversationId;
    private bool          isAwaitingResponse = false;
    private List<string>  userConvs = new List<string>();

    void Awake()
    {
        CurrentUserId = PlayerPrefs.GetString("Nickname", "");
        Debug.Log($"[ChatManager] CurrentUserId = '{CurrentUserId}'");
    }

    void Start()
    {
        // bersihkan UI dan reset
        ClearChat();
        currentConversationId = null;

        // hook tombol
        newChatButton.onClick.AddListener(OnNewChatClicked);
        sendButton.onClick.AddListener(OnSendClicked);

        // ambil semua conversations milik user ini
        StartCoroutine(
            ServiceManager.Instance.ChatService.FetchUserConversations(
                CurrentUserId,
                convIds =>
                {
                    PopulateHistoryButtons(convIds);
                    userConvs = convIds.ToList();
                },
                err => Debug.LogError("Fetch conv IDs failed: " + err)
            )
        );

        deleteChatSetting.SetActive(false);
        deleteNoButton.onClick.AddListener(() =>
        {
            deleteChatSetting.SetActive(false);
            pendingDeleteId = null;
        });
        deleteYesButton.onClick.AddListener(ConfirmDeleteConversation);
    }

    private void PopulateHistoryButtons(string[] convIds)
    {
        // 1) Clear existing
        for (int i = chatHistoryParent.childCount - 1; i >= 0; i--)
            Destroy(chatHistoryParent.GetChild(i).gameObject);

        // 2) Re‐create one button per conversation
        for (int i = 0; i < convIds.Length; i++)
        {
            string convId = convIds[i];
            var go  = Instantiate(historyButtonPrefab, chatHistoryParent);
            var hb  = go.GetComponent<HistoryButton>();
            hb.SetConversationId(convId);

            // sementara pakai placeholder…
            hb.SetLabel($"Chat {i+1}");

            // → lalu request snippet dari server
            StartCoroutine(
                ServiceManager.Instance.ChatService.FetchFirstMessage(
                    convId,
                    firstMsg =>
                    {
                        const int MAX = 20;
                        string snippet = string.IsNullOrEmpty(firstMsg)
                            ? $"Chat {i+1}"
                            : (firstMsg.Length > MAX
                                ? firstMsg.Substring(0, MAX) + "…"
                                : firstMsg);
                        hb.SetLabel(snippet);
                    },
                    err =>
                    {
                        Debug.LogWarning($"FetchFirstMessage failed for {convId}: {err}");
                        // biarkan label default
                    }
                )
            );

            // hookup delete button seperti sebelumnya…
            var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
            if (delBtn != null)
            {
                Debug.Log("[ChatManager] Hooking up delete button for " + convId);
                delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
            }
        }
    }

    public void OnHistoryClicked(string conversationId)
    {
        Debug.Log($"[ChatManager] OnHistoryClicked('{conversationId}')");
        currentConversationId = conversationId;
        ClearChat();

        StartCoroutine(
            ServiceManager.Instance.ChatService.FetchConversationWithMessages(
                conversationId,
                CurrentUserId,
                msgs =>
                {
                    foreach (var m in msgs)
                        CreateBubble(m.message, m.sender == CurrentUserId);
                },
                err => Debug.LogError("Fetch conversation failed: " + err)
            )
        );
    }

    private void OnNewChatClicked()
    {
        Debug.Log("[ChatManager] Starting new chat");
        ClearChat();
        currentConversationId = null;
    }

    private void OnSendClicked()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || isAwaitingResponse)
            return;

        inputField.text = "";
        CreateBubble(text, true);
        Debug.Log($"[ChatManager] OnSendClicked text='{text}' convoId='{currentConversationId}'");

        // first message of a brand-new conversation?
        if (string.IsNullOrEmpty(currentConversationId))
        {
            // 1) Cari angka tertinggi dari semua userConvs
            var regex = new Regex(@"cv(\d+)$");
            int nextIdx = userConvs
                .Select(id =>
                {
                    var m = regex.Match(id);
                    return m.Success ? int.Parse(m.Groups[1].Value) : 0;
                })
                .DefaultIfEmpty(0)
                .Max() + 1;

            // 2) Bangun ID unik = "{UserId}_cvNN"
            currentConversationId = $"{CurrentUserId}_cv{nextIdx:00}";
            userConvs.Add(currentConversationId);

            // 3) Tambah tombol history baru
            var go = Instantiate(historyButtonPrefab, chatHistoryParent);
            var hb = go.GetComponent<HistoryButton>();
            hb.SetConversationId(currentConversationId);
            const int SNIP_MAX = 20;
            string snippet = text.Length > SNIP_MAX
                ? text.Substring(0, SNIP_MAX) + "..."
                : text;
            hb.SetLabel(snippet);

            // 4) Simpan conversation ke DB dulu
            StartCoroutine(
                ServiceManager.Instance.ChatService.CreateConversation(
                    currentConversationId,
                    CurrentUserId,
                    onSuccess: () => StartCoroutine(SendUserMessage(text)),
                    onError: err => Debug.LogError("CreateConversation failed: " + err)
                )
            );
        }
        else
        {
            // sudah ada convo, langsung insert message
            StartCoroutine(SendUserMessage(text));
        }
    }

    private IEnumerator SendUserMessage(string text)
    {
        yield return ServiceManager.Instance.ChatService.InsertMessage(
            currentConversationId,
            CurrentUserId,
            text,
            onSuccess:    () => StartCoroutine(HandleAITurn(text)),
            onError:      err => Debug.LogError("Insert message failed: " + err)
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

        var typingGO   = Instantiate(aiBubblePrefab, chatContentParent);
        var typingCtrl = typingGO.GetComponent<ChatBubbleController>();
        var anim       = StartCoroutine(AnimateTyping(typingCtrl));

        yield return OllamaService.SendPrompt(userMessage, response =>
        {
            StopCoroutine(anim);
            Destroy(typingGO);
            CreateBubble(response, false);

            // simpan AI reply
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

    public void OnDeleteClicked(string conversationId)
    {
        // simpan id, tampilkan panel
        pendingDeleteId = conversationId;
        deleteChatSetting.SetActive(true);
    }

    private void ConfirmDeleteConversation()
    {
        if (string.IsNullOrEmpty(pendingDeleteId))
            return;

        // 1) hapus messages dulu
        StartCoroutine(
            ServiceManager.Instance.ChatService.DeleteMessagesForConversation(
                pendingDeleteId,
                onSuccess: () => {
                    // 2) lalu hapus conversation row
                    StartCoroutine(
                        ServiceManager.Instance.ChatService.DeleteConversation(
                            pendingDeleteId,
                            onSuccess: () =>
                            {
                                // tutup panel
                                deleteChatSetting.SetActive(false);

                                // refresh sidebar
                                StartCoroutine(
                                    ServiceManager.Instance.ChatService.FetchUserConversations(
                                        CurrentUserId,
                                        convIds => {
                                            PopulateHistoryButtons(convIds);
                                        },
                                        err => Debug.LogError("Fetch conv IDs failed: " + err)
                                    )
                                );

                                // jika view sedang menunjukkan yang di‐delete, kosongkan chat
                                if (pendingDeleteId == currentConversationId)
                                {
                                    ClearChat();
                                    currentConversationId = null;
                                }

                                pendingDeleteId = null;
                            },
                            onError: err => Debug.LogError("DeleteConversation failed: " + err)
                        )
                    );
                },
                onError: err => Debug.LogError("DeleteMessages failed: " + err)
            )
        );
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