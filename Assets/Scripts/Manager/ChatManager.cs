using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EMILIA.Data;

public class ChatManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Prefabs & UI References")]
    [SerializeField] private GameObject     _userBubblePrefab;
    [SerializeField] private GameObject     _aiBubblePrefab;
    [SerializeField] private GameObject     _historyButtonPrefab;
    [SerializeField] private Transform      _chatContentParent;
    [SerializeField] private Transform      _chatHistoryParent;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button         _newChatButton;
    [SerializeField] private Button         _sendButton;

    [Header("Delete Confirmation UI")]
    [SerializeField] private GameObject     _deleteChatSetting;
    [SerializeField] private Button         _deleteYesButton;
    [SerializeField] private Button         _deleteNoButton;

    #endregion

    #region Constants & Fields

    private const string PrefKeyNickname     = "Nickname";
    private const int    SnippetMaxLength    = 20;
    private const string TypingPlaceholderId = "__TYPING__";
    private const string NewChatLabel        = "New Chat";
    private static readonly Regex ConversationRegex =
        new Regex(@"cv(\d+)$", RegexOptions.Compiled);

    [HideInInspector] public string CurrentUserId;

    private string _currentConversationId;
    private string _pendingDeleteId;
    private bool   _isAwaitingResponse;

    // Cache pesan per percakapan (termasuk placeholder)
    private readonly Dictionary<string, List<Message>> _messageCache =
        new Dictionary<string, List<Message>>();

    // Track convo yang sedang menunggu reply AI
    private readonly HashSet<string> _isTyping = new HashSet<string>();

    // Untuk generate ID baru
    private readonly List<string> _userConvs = new List<string>();

    // Topic guards & cache (session-level)
    private readonly Dictionary<string, string> _topicCache     = new Dictionary<string, string>();
    private readonly HashSet<string>            _topicRequested = new HashSet<string>(); // in-flight / done

    // Summary guard: sudah diringkas sampai pair ke-berapa per convo
    private readonly Dictionary<string, int> _lastSummarizedPairCount = new Dictionary<string, int>();

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        LoadCurrentUser();
        InitializeUI();
    }

    private void Start()
    {
        FetchAndPopulateHistory();
    }

    #endregion

    #region Initialization & Session

    private void LoadCurrentUser()
    {
        var nick = PlayerPrefs.GetString(PrefKeyNickname, "");
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            CurrentUserId = nick;
        }
        else if (CurrentUserId != nick)
        {
            OnUserChanged(nick);
            return;
        }

        Debug.Log($"[ChatManager] CurrentUserId = '{CurrentUserId}'");
        _currentConversationId = null;
        ClearChat();
    }

    private void InitializeUI()
    {
        _deleteChatSetting.SetActive(false);

        _newChatButton.onClick.AddListener(OnNewChatClicked);
        _sendButton    .onClick.AddListener(OnSendClicked);
        _deleteNoButton.onClick.AddListener(() =>
        {
            _deleteChatSetting.SetActive(false);
            _pendingDeleteId = null;
        });
        _deleteYesButton.onClick.AddListener(ConfirmDeleteConversation);
    }

    /// <summary> Dipanggil saat user berganti (logout/login). </summary>
    public void OnUserChanged(string newUserId)
    {
        if (string.Equals(CurrentUserId, newUserId, StringComparison.Ordinal)) return;
        CurrentUserId = newUserId;

        _messageCache.Clear();
        _isTyping.Clear();
        _topicCache.Clear();
        _topicRequested.Clear();
        _lastSummarizedPairCount.Clear();
        _userConvs.Clear();
        _currentConversationId = null;
        ClearChat();

        FetchAndPopulateHistory();
    }

    private void ResetSessionState()
    {
        _messageCache.Clear();
        _isTyping.Clear();
        _topicCache.Clear();
        _topicRequested.Clear();
        _lastSummarizedPairCount.Clear();
        _userConvs.Clear();
        _currentConversationId = null;
        ClearChat();
    }

    #endregion

    #region History Sidebar

    private void FetchAndPopulateHistory()
    {
        StartCoroutine(ServiceManager.Instance.ChatService.FetchUserConversations(
            CurrentUserId,
            convIds =>
            {
                PopulateHistoryButtons(convIds);
                _userConvs.Clear();
                _userConvs.AddRange(convIds);
            },
            err => Debug.LogError($"Fetch conv IDs failed: {err}")
        ));
    }

    private void PopulateHistoryButtons(string[] convIds)
    {
        for (int i = _chatHistoryParent.childCount - 1; i >= 0; i--)
            Destroy(_chatHistoryParent.GetChild(i).gameObject);

        for (int i = 0; i < convIds.Length; i++)
            SetupHistoryButton(convIds[i], i);
    }

    private void SetupHistoryButton(string convId, int index)
    {
        var go = Instantiate(_historyButtonPrefab, _chatHistoryParent);
        var hb = go.GetComponent<HistoryButton>();
        hb.SetConversationId(convId);

        var placeholderText = $"Chat {index + 1}";
        hb.SetLabel(placeholderText);

        // 0) Pakai title dari DB jika sudah ada
        var existingTitle = ServiceManager.Instance.ChatService.GetConversationTitle(convId);
        if (!string.IsNullOrWhiteSpace(existingTitle))
        {
            _topicCache[convId] = existingTitle; // seed cache session
            hb.SetLabel(existingTitle);
        }
        else if (_topicCache.TryGetValue(convId, out var sessionTopic) && !string.IsNullOrWhiteSpace(sessionTopic))
        {
            hb.SetLabel(sessionTopic);
        }

        // 1) Untuk history lama: kalau ada sepasang user+bot dan title belum ada di DB, generate topic SEKALI.
        if (string.IsNullOrWhiteSpace(existingTitle))
        {
            StartCoroutine(ServiceManager.Instance.ChatService.FetchConversationWithMessages(
                convId,
                CurrentUserId,
                msgs =>
                {
                    var firstUser = msgs.FirstOrDefault(m => m.Sender != "Bot");
                    var firstBot  = msgs.FirstOrDefault(m => m.Sender == "Bot");
                    if (firstUser != null && firstBot != null)
                        TryGenerateTopicOnce(convId, firstUser.Text, firstBot.Text, hb);
                    else
                        hb.SetLabel(NewChatLabel); // belum ada balasan bot → tampil "New Chat"
                },
                err => Debug.LogWarning($"Fetch conv for topic failed: {err}")
            ));
        }

        var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null)
            delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
    }

    private static string FormatSnippet(string msg, int fallbackIndex)
    {
        if (string.IsNullOrEmpty(msg))
            return $"Chat {fallbackIndex}";
        return msg.Length <= SnippetMaxLength
            ? msg
            : msg.Substring(0, SnippetMaxLength) + "…";
    }

    public void OnHistoryClicked(string conversationId)
    {
        _currentConversationId = conversationId;
        ClearChat();

        if (_messageCache.TryGetValue(conversationId, out var cached))
            RebuildChatUI(conversationId);

        StartCoroutine(ServiceManager.Instance.ChatService.FetchConversationWithMessages(
            conversationId,
            CurrentUserId,
            msgs =>
            {
                _messageCache[conversationId] = new List<Message>(msgs);

                if (_isTyping.Contains(conversationId))
                {
                    _messageCache[conversationId].Add(new Message {
                        Id             = TypingPlaceholderId,
                        ConversationId = conversationId,
                        Sender         = "Bot",
                        Text           = null,
                        SentAt         = DateTime.UtcNow
                    });
                }

                RebuildChatUI(conversationId);
            },
            err => Debug.LogError($"Fetch conversation failed: {err}")
        ));
    }

    #endregion

    #region Sending & New Conversation

    private void OnNewChatClicked()
    {
        ClearChat();
        _currentConversationId = null;
    }

    private void OnSendClicked()
    {
        var text = _inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || _isAwaitingResponse)
            return;

        _inputField.text = "";

        CreateBubble(text, true);

        if (string.IsNullOrEmpty(_currentConversationId))
            StartNewConversation(text);
        else
            StartCoroutine(SendUserMessage(text, _currentConversationId));
    }

    private void StartNewConversation(string text)
    {
        var convoId = GenerateConversationId();
        _currentConversationId = convoId;
        _userConvs.Add(convoId);

        _messageCache[convoId] = new List<Message>();

        AddHistoryButtonForNew(convoId, text);

        StartCoroutine(ServiceManager.Instance.ChatService.CreateConversation(
            convoId,
            CurrentUserId,
            onSuccess: () => StartCoroutine(SendUserMessage(text, convoId)),
            onError:   err => Debug.LogError($"CreateConversation failed: {err}")
        ));
    }

    private string GenerateConversationId()
    {
        int nextIdx = _userConvs
            .Select(id =>
            {
                var m = ConversationRegex.Match(id);
                return m.Success ? int.Parse(m.Groups[1].Value) : 0;
            })
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{CurrentUserId}_cv{nextIdx:00}";
    }

    private void AddHistoryButtonForNew(string convId, string initialMsg)
    {
        var go = Instantiate(_historyButtonPrefab, _chatHistoryParent);
        var hb = go.GetComponent<HistoryButton>();
        hb.SetConversationId(convId);

        // Tampilkan "New Chat" saat belum ada balasan bot
        hb.SetLabel(NewChatLabel);
        // Pastikan tombol baru langsung di paling atas
        hb.transform.SetAsFirstSibling();

        var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null)
            delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
    }

    private IEnumerator SendUserMessage(string text, string convoId)
    {
        var userMsg = new Message {
            Id             = Guid.NewGuid().ToString(),
            ConversationId = convoId,
            Sender         = CurrentUserId,
            Text           = text,
            SentAt         = DateTime.UtcNow
        };

        _messageCache[convoId].Add(userMsg);

        yield return ServiceManager.Instance.ChatService.InsertMessage(
            convoId,
            CurrentUserId,
            text,
            onSuccess: () => StartCoroutine(HandleAITurn(text, convoId)),
            onError:   err => Debug.LogError($"Insert message failed: {err}")
        );
    }

    #endregion

    #region AI Handling & Typing

    private IEnumerator HandleAITurn(string userMessage, string convoId)
    {
        _isAwaitingResponse = true;
        _isTyping.Add(convoId);

        _messageCache[convoId].Add(new Message {
            Id             = TypingPlaceholderId,
            ConversationId = convoId,
            Sender         = "Bot",
            Text           = null,
            SentAt         = DateTime.UtcNow
        });

        if (_currentConversationId == convoId)
            RebuildChatUI(convoId);

        // === /chat ===
        yield return ServiceManager.Instance.ChatApi.SendPrompt(
            username: CurrentUserId,
            question: userMessage,
            audioBytes: null,
            audioFileName: null,
            audioMime: null,
            onSuccess: response =>
            {
                _isTyping.Remove(convoId);
                _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);

                var aiMsg = new Message {
                    Id             = Guid.NewGuid().ToString(),
                    ConversationId = convoId,
                    Sender         = "Bot",
                    Text           = response,
                    SentAt         = DateTime.UtcNow
                };
                _messageCache[convoId].Add(aiMsg);

                if (_currentConversationId == convoId)
                    RebuildChatUI(convoId);

                SaveAIMessage(response, convoId);
                _isAwaitingResponse = false;

                Debug.LogError("generate topic");

                // === /topic sekali, lalu simpan ke DB ===
                TryGenerateTopicOnce(convoId, userMessage, response);

                Debug.LogError("summarize every 2 pairs");

                // === /summary tiap 2,4,6,... pair ===
                TrySummarizeEveryTwoPairs(convoId);
            },
            onError: err =>
            {
                Debug.LogError($"Chat API error: {err}");
                _isTyping.Remove(convoId);
                _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);
                _isAwaitingResponse = false;
                if (_currentConversationId == convoId)
                    RebuildChatUI(convoId);
            }
        );
    }

    private void SaveAIMessage(string response, string convoId)
    {
        StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(
            convoId,
            "Bot",
            response,
            onSuccess: () =>
            {
                var btn = _chatHistoryParent
                    .GetComponentsInChildren<HistoryButton>()
                    .FirstOrDefault(h => h.ConversationId == convoId);
                if (btn != null) btn.transform.SetAsFirstSibling();
            },
            onError: err => Debug.LogError($"Save AI message failed: {err}")
        ));
    }

    /// <summary>
    /// Minta topic ke /topic hanya SEKALI per conversation.
    /// - Cek DB dulu (title). Jika ada → pakai & cache.
    /// - Kalau belum ada dan belum in-flight → request; simpan ke DB + cache.
    /// </summary>
    private void TryGenerateTopicOnce(string convoId, string userText, string botText, HistoryButton hb = null)
    {
        if (string.IsNullOrWhiteSpace(userText) || string.IsNullOrWhiteSpace(botText))
            return;

        // 1) Cek DB — kalau sudah ada title, jangan request lagi
        var dbTitle = ServiceManager.Instance.ChatService.GetConversationTitle(convoId);
        if (!string.IsNullOrWhiteSpace(dbTitle))
        {
            _topicCache[convoId] = dbTitle;
            (hb ?? _chatHistoryParent.GetComponentsInChildren<HistoryButton>(true)
                                     .FirstOrDefault(h => h.ConversationId == convoId))
                                     ?.SetLabel(dbTitle);
            return;
        }

        // 2) Cek cache/guard session
        if (_topicCache.ContainsKey(convoId) || _topicRequested.Contains(convoId))
            return;

        _topicRequested.Add(convoId);

        StartCoroutine(ServiceManager.Instance.TopicApi.GetTopic(
            userText,
            botText,
            onSuccess: topic =>
            {
                _topicRequested.Remove(convoId);
                if (!string.IsNullOrWhiteSpace(topic))
                {
                    _topicCache[convoId] = topic;

                    // Persist ke DB
                    ServiceManager.Instance.ChatService.UpdateConversationTitle(convoId, topic);

                    // Update UI
                    (hb ?? _chatHistoryParent.GetComponentsInChildren<HistoryButton>(true)
                                             .FirstOrDefault(h => h.ConversationId == convoId))
                                             ?.SetLabel(topic);
                }
            },
            onError: err =>
            {
                _topicRequested.Remove(convoId);
                Debug.LogWarning($"[TopicOnce] {convoId}: {err}");
            }
        ));
    }

    /// <summary>
    /// Hitung pasangan user→bot dari URUTAN pesan (independen dari CurrentUserId).
    /// Tembak /summary setiap kali jumlah pasangan menjadi genap (2,4,6,...).
    /// </summary>
    private void TrySummarizeEveryTwoPairs(string convoId)
    {
        Debug.LogError($"[SUMMARY] Checking convo '{convoId}' for pairs...");

        if (!_messageCache.TryGetValue(convoId, out var msgs) || msgs == null) return;

        // Urutkan by waktu
        var ordered = msgs.OrderBy(m => m.SentAt).ToList();

        int pairs = 0;
        bool waitingForBot = false;

        foreach (var m in ordered)
        {
            // Anggap semua selain "Bot" = pesan user
            if (m.Sender == "Bot")
            {
                if (waitingForBot) { pairs++; waitingForBot = false; }
            }
            else
            {
                waitingForBot = true;
            }
        }

        Debug.LogError($"[SUMMARY] {convoId} pairs={pairs}");

        // Tembak hanya saat genap: 2,4,6,...
        if (pairs < 2 || (pairs % 2 != 0)) return;

        int last = _lastSummarizedPairCount.TryGetValue(convoId, out var v) ? v : 0;
        if (pairs == last) return; // sudah disummary untuk jumlah ini

        _lastSummarizedPairCount[convoId] = pairs;

        // Pastikan ServiceManager.SummaryApi tidak null
        if (ServiceManager.Instance?.SummaryApi == null)
        {
            Debug.LogError("[SUMMARY] SummaryApi belum terinisialisasi di ServiceManager.");
            return;
        }

        StartCoroutine(ServiceManager.Instance.SummaryApi.RequestSummary(
            convoId,
            onSuccess: text =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // simpan ke tabel summary, TANPA render ke UI
                    StartCoroutine(ServiceManager.Instance.ChatService.InsertSummary(
                        convoId,
                        text,
                        onSuccess: () => Debug.LogWarning($"[SUMMARY] saved for {convoId}"),
                        onError:   err => Debug.LogWarning($"[SUMMARY] save failed: {err}")
                    ));
                }
            },
            onError:   err  => Debug.LogWarning($"[SUMMARY] ({convoId}) ERROR: {err}")
        ));
    }

    #endregion

    #region Delete Conversation

    public void OnDeleteClicked(string conversationId)
    {
        _pendingDeleteId = conversationId;
        _deleteChatSetting.SetActive(true);
    }

    private void ConfirmDeleteConversation()
    {
        if (string.IsNullOrEmpty(_pendingDeleteId)) return;

        StartCoroutine(ServiceManager.Instance.ChatService.DeleteMessagesForConversation(
            _pendingDeleteId,
            onSuccess: () => StartCoroutine(ProceedDeleteConversation()),
            onError:   err => Debug.LogError($"DeleteMessages failed: {err}")
        ));
    }

    private IEnumerator ProceedDeleteConversation()
    {
        yield return ServiceManager.Instance.ChatService.DeleteConversation(
            _pendingDeleteId,
            onSuccess: () =>
            {
                _deleteChatSetting.SetActive(false);
                FetchAndPopulateHistory();

                if (_pendingDeleteId == _currentConversationId)
                {
                    ClearChat();
                    _currentConversationId = null;
                }
                _pendingDeleteId = null;
            },
            onError: err => Debug.LogError($"DeleteConversation failed: {err}")
        );
    }

    #endregion

    #region UI Rendering

    private void RebuildChatUI(string convoId)
    {
        ClearChat();

        foreach (var m in _messageCache[convoId])
        {
            if (m.Id == TypingPlaceholderId)
            {
                var go   = Instantiate(_aiBubblePrefab, _chatContentParent);
                var ctrl = go.GetComponent<ChatBubbleController>();
                StartCoroutine(AnimateTyping(ctrl));
            }
            else
            {
                bool isUser = m.Sender != "Bot";
                CreateBubble(m.Text, isUser);
            }
        }
    }

    private void CreateBubble(string message, bool isUser)
    {
        var prefab = isUser ? _userBubblePrefab : _aiBubblePrefab;
        var go     = Instantiate(prefab, _chatContentParent);
        go.GetComponent<ChatBubbleController>()?.SetText(message);
    }

    private void ClearChat()
    {
        for (int i = _chatContentParent.childCount - 1; i >= 0; i--)
            Destroy(_chatContentParent.GetChild(i).gameObject);
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

    #endregion
}