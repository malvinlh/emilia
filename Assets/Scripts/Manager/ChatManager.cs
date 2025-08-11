using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EMILIA.Data;

public class ChatManager : MonoBehaviour
{
    #region Inspector

    [Header("Prefabs & UI References")]
    [SerializeField] private GameObject     _userBubblePrefab;
    [SerializeField] private GameObject     _aiBubblePrefab;
    [SerializeField] private GameObject     _historyButtonPrefab;
    [SerializeField] private Transform      _chatContentParent;
    [SerializeField] private Transform      _chatHistoryParent;
    [SerializeField] private TMP_InputField _inputField;

    [Header("Input Buttons")]
    [SerializeField] private Button         _newChatButton;
    [SerializeField] private Button         _sendButton;          // /chat
    [SerializeField] private Button         _reasoningSendButton; // /agentic

    [Header("Delete Confirmation UI")]
    [SerializeField] private GameObject     _deleteChatSetting;
    [SerializeField] private Button         _deleteYesButton;
    [SerializeField] private Button         _deleteNoButton;

    #endregion

    #region Constants & State

    private const string LogTag              = "[ChatManager]";
    private const string PrefKeyNickname     = "Nickname";
    private const int    SnippetMaxLength    = 20;
    private const string TypingPlaceholderId = "__TYPING__";
    private const string NewChatLabel        = "New Chat";

    private const string ReasoningSender     = "Reasoning";
    private const string BotSender           = "Bot";

    private static readonly Regex ConversationRegex =
        new Regex(@"cv(\d+)$", RegexOptions.Compiled);

    [HideInInspector] public string CurrentUserId;

    private string _currentConversationId;
    private string _pendingDeleteId;
    private bool   _isAwaitingResponse;

    private readonly Dictionary<string, List<Message>> _messageCache = new();
    private readonly HashSet<string> _isTyping = new();
    private readonly List<string> _userConvs = new();

    private readonly Dictionary<string, string> _topicCache     = new();
    private readonly HashSet<string>            _topicRequested = new();
    private readonly Dictionary<string, int>    _lastSummarizedPairCount = new();

    #endregion

    #region Unity lifecycle

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

    #region Init & Session

    private void LoadCurrentUser()
    {
        var nick = PlayerPrefs.GetString(PrefKeyNickname, "");
        if (string.IsNullOrEmpty(CurrentUserId)) CurrentUserId = nick;
        else if (CurrentUserId != nick) { OnUserChanged(nick); return; }

        Debug.Log($"{LogTag} CurrentUserId = '{CurrentUserId}'");
        _currentConversationId = null;
        ClearChat();
    }

    private void InitializeUI()
    {
        _deleteChatSetting.SetActive(false);

        _newChatButton.onClick.AddListener(OnNewChatClicked);
        _sendButton?.onClick.AddListener(() => OnSendClicked(useAgentic:false));
        _reasoningSendButton?.onClick.AddListener(() => OnSendClicked(useAgentic:true));

        _deleteNoButton.onClick.AddListener(() =>
        {
            _deleteChatSetting.SetActive(false);
            _pendingDeleteId = null;
        });
        _deleteYesButton.onClick.AddListener(ConfirmDeleteConversation);
    }

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

    #endregion

    #region History

    private void FetchAndPopulateHistory()
    {
        StartCoroutine(ServiceManager.Instance.ChatService.FetchUserConversations(
            CurrentUserId,
            ids =>
            {
                PopulateHistoryButtons(ids);
                _userConvs.Clear();
                _userConvs.AddRange(ids);
            },
            err => Debug.LogError($"{LogTag} Fetch conv IDs failed: {err}")
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

        var placeholder = $"Chat {index + 1}";
        hb.SetLabel(placeholder);

        var existingTitle = ServiceManager.Instance.ChatService.GetConversationTitle(convId);
        if (!string.IsNullOrWhiteSpace(existingTitle))
        {
            _topicCache[convId] = existingTitle;
            hb.SetLabel(existingTitle);
        }
        else if (_topicCache.TryGetValue(convId, out var sessionTopic) && !string.IsNullOrWhiteSpace(sessionTopic))
        {
            hb.SetLabel(sessionTopic);
        }

        if (string.IsNullOrWhiteSpace(existingTitle))
        {
            StartCoroutine(ServiceManager.Instance.ChatService.FetchConversationWithMessages(
                convId,
                CurrentUserId,
                msgs =>
                {
                    var firstUser = msgs.FirstOrDefault(m => m.Sender != BotSender && m.Sender != ReasoningSender);
                    var firstBot  = msgs.FirstOrDefault(m => m.Sender == BotSender);
                    if (firstUser != null && firstBot != null)
                        TryGenerateTopicOnce(convId, firstUser.Text, firstBot.Text, hb);
                    else
                        hb.SetLabel(NewChatLabel);
                },
                err => Debug.LogWarning($"{LogTag} Fetch conv for topic failed: {err}")
            ));
        }

        var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null) delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
    }

    public void OnHistoryClicked(string conversationId)
    {
        _currentConversationId = conversationId;
        ClearChat();

        if (_messageCache.TryGetValue(conversationId, out var _))
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
                        Sender         = BotSender,
                        Text           = null,
                        SentAt         = DateTime.UtcNow
                    });
                }
                RebuildChatUI(conversationId);
            },
            err => Debug.LogError($"{LogTag} Fetch conversation failed: {err}")
        ));
    }

    #endregion

    #region Sending

    private void OnNewChatClicked()
    {
        ClearChat();
        _currentConversationId = null;
    }

    private void OnSendClicked(bool useAgentic)
    {
        var text = _inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || _isAwaitingResponse) return;

        _inputField.text = "";
        CreateBubble(text, true);

        if (string.IsNullOrEmpty(_currentConversationId))
            StartNewConversation(text, useAgentic);
        else
            StartCoroutine(SendUserMessage(text, _currentConversationId, useAgentic));
    }

    private void StartNewConversation(string firstUserMsg, bool useAgentic)
    {
        var convoId = GenerateConversationId();
        _currentConversationId = convoId;
        _userConvs.Add(convoId);
        _messageCache[convoId] = new List<Message>();

        AddHistoryButtonForNew(convoId);

        StartCoroutine(ServiceManager.Instance.ChatService.CreateConversation(
            convoId,
            CurrentUserId,
            onSuccess: () => StartCoroutine(SendUserMessage(firstUserMsg, convoId, useAgentic)),
            onError:   err => Debug.LogError($"{LogTag} CreateConversation failed: {err}")
        ));
    }

    private string GenerateConversationId()
    {
        int nextIdx = _userConvs
            .Select(id => {
                var m = ConversationRegex.Match(id);
                return m.Success ? int.Parse(m.Groups[1].Value) : 0;
            })
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{CurrentUserId}_cv{nextIdx:00}";
    }

    private void AddHistoryButtonForNew(string convId)
    {
        var go = Instantiate(_historyButtonPrefab, _chatHistoryParent);
        var hb = go.GetComponent<HistoryButton>();
        hb.SetConversationId(convId);
        hb.SetLabel(NewChatLabel);
        hb.transform.SetAsFirstSibling();

        var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null) delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
    }

    private IEnumerator SendUserMessage(string text, string convoId, bool useAgentic)
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
            onSuccess: () => StartCoroutine(HandleAITurn(text, convoId, useAgentic)),
            onError:   err => Debug.LogError($"{LogTag} Insert message failed: {err}")
        );
    }

    #endregion

    #region AI Handling

    private IEnumerator HandleAITurn(string userMessage, string convoId, bool useAgentic)
    {
        _isAwaitingResponse = true;
        _isTyping.Add(convoId);

        _messageCache[convoId].Add(new Message {
            Id             = TypingPlaceholderId,
            ConversationId = convoId,
            Sender         = BotSender,
            Text           = null,
            SentAt         = DateTime.UtcNow
        });

        if (_currentConversationId == convoId) RebuildChatUI(convoId);

        if (useAgentic)
        {
            // ===== /agentic =====
            yield return ServiceManager.Instance.AgenticApi.Send(
                userId: CurrentUserId,
                username: CurrentUserId,
                question: userMessage,
                onSuccess: res  => OnAgenticSuccess(convoId, userMessage, res),
                onError:   err  => OnAIError(convoId, err)
            );
        }
        else
        {
            // ===== /chat (biasa) =====
            yield return ServiceManager.Instance.ChatApi.SendPrompt(
                username: CurrentUserId,
                question: userMessage,
                audioBytes: null, audioFileName: null, audioMime: null,
                onSuccess: resp => OnPlainChatSuccess(convoId, userMessage, resp),
                onError:   err  => OnAIError(convoId, err)
            );
        }
    }

    private void OnAgenticSuccess(string convoId, string userMessage, APIAgenticService.AgenticResult res)
    {
        _isTyping.Remove(convoId);
        _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);

        // Simpan RAW ke DB & cache: Reasoning lalu Bot
        var reasoningRaw = (res.reasoning ?? "").Trim();
        var responseRaw  = (res.response  ?? "").Trim();

        // 1) Reasoning (sender = "Reasoning")
        AddMessageToCache(convoId, ReasoningSender, reasoningRaw);
        StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(
            convoId, ReasoningSender, reasoningRaw, null,
            err => Debug.LogWarning($"{LogTag} Save reasoning failed: {err}")
        ));

        // 2) Response (sender = "Bot")
        AddMessageToCache(convoId, BotSender, responseRaw);
        SaveAIMessage(responseRaw, convoId);

        if (_currentConversationId == convoId) RebuildChatUI(convoId);
        _isAwaitingResponse = false;

        // Judul pakai final response
        TryGenerateTopicOnce(convoId, userMessage, responseRaw);
        TrySummarizeEveryTwoPairs(convoId);
    }

    private void OnPlainChatSuccess(string convoId, string userMessage, string response)
    {
        _isTyping.Remove(convoId);
        _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);

        AddMessageToCache(convoId, BotSender, response);
        SaveAIMessage(response, convoId);

        if (_currentConversationId == convoId) RebuildChatUI(convoId);
        _isAwaitingResponse = false;

        TryGenerateTopicOnce(convoId, userMessage, response);
        TrySummarizeEveryTwoPairs(convoId);
    }

    private void OnAIError(string convoId, string err)
    {
        Debug.LogError($"{LogTag} AI error: {err}");
        _isTyping.Remove(convoId);
        _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);
        _isAwaitingResponse = false;
        if (_currentConversationId == convoId) RebuildChatUI(convoId);
    }

    private void AddMessageToCache(string convoId, string sender, string text)
    {
        _messageCache[convoId].Add(new Message {
            Id             = Guid.NewGuid().ToString(),
            ConversationId = convoId,
            Sender         = sender,
            Text           = text,
            SentAt         = DateTime.UtcNow
        });
    }

    private void SaveAIMessage(string textToPersist, string convoId)
    {
        StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(
            convoId, BotSender, textToPersist,
            onSuccess: () =>
            {
                var btn = _chatHistoryParent
                    .GetComponentsInChildren<HistoryButton>()
                    .FirstOrDefault(h => h.ConversationId == convoId);
                if (btn != null) btn.transform.SetAsFirstSibling();
            },
            onError: err => Debug.LogError($"{LogTag} Save AI message failed: {err}")
        ));
    }

    #endregion

    #region Topic & Summary

    private void TryGenerateTopicOnce(string convoId, string userText, string botText, HistoryButton hb = null)
    {
        if (string.IsNullOrWhiteSpace(userText) || string.IsNullOrWhiteSpace(botText)) return;

        var dbTitle = ServiceManager.Instance.ChatService.GetConversationTitle(convoId);
        if (!string.IsNullOrWhiteSpace(dbTitle))
        {
            _topicCache[convoId] = dbTitle;
            (hb ?? _chatHistoryParent.GetComponentsInChildren<HistoryButton>(true)
                                     .FirstOrDefault(h => h.ConversationId == convoId))
                                     ?.SetLabel(dbTitle);
            return;
        }

        if (_topicCache.ContainsKey(convoId) || _topicRequested.Contains(convoId)) return;
        _topicRequested.Add(convoId);

        StartCoroutine(ServiceManager.Instance.TopicApi.GetTopic(
            userText, botText,
            onSuccess: topic =>
            {
                _topicRequested.Remove(convoId);
                if (!string.IsNullOrWhiteSpace(topic))
                {
                    _topicCache[convoId] = topic;
                    ServiceManager.Instance.ChatService.UpdateConversationTitle(convoId, topic);
                    (hb ?? _chatHistoryParent.GetComponentsInChildren<HistoryButton>(true)
                                             .FirstOrDefault(h => h.ConversationId == convoId))
                                             ?.SetLabel(topic);
                }
            },
            onError: err =>
            {
                _topicRequested.Remove(convoId);
                Debug.LogWarning($"{LogTag} [TopicOnce] {convoId}: {err}");
            }
        ));
    }

    /// <summary>
    /// Hitung pasangan user→bot (abaikan Reasoning) dan tembak /summary tiap kali genap 2,4,6...
    /// </summary>
    private void TrySummarizeEveryTwoPairs(string convoId)
    {
        if (!_messageCache.TryGetValue(convoId, out var msgs) || msgs == null) return;

        var ordered = msgs.OrderBy(m => m.SentAt).ToList();

        bool IsUserMsg(Message m) => m.Sender != BotSender && m.Sender != ReasoningSender;
        bool IsBotMsg (Message m) => m.Sender == BotSender;

        int pairs = 0;
        bool waitingForBot = false;

        foreach (var m in ordered)
        {
            if (IsBotMsg(m))
            {
                if (waitingForBot) { pairs++; waitingForBot = false; }
            }
            else if (IsUserMsg(m))
            {
                waitingForBot = true;
            }
            // Reasoning diabaikan
        }

        if (pairs < 2 || (pairs % 2 != 0)) return;

        int last = _lastSummarizedPairCount.TryGetValue(convoId, out var v) ? v : 0;
        if (pairs == last) return;

        _lastSummarizedPairCount[convoId] = pairs;

        if (ServiceManager.Instance?.SummaryApi == null)
        {
            Debug.LogWarning($"{LogTag} [SUMMARY] SummaryApi belum terinisialisasi.");
            return;
        }

        StartCoroutine(ServiceManager.Instance.SummaryApi.RequestSummary(
            convoId,
            onSuccess: text =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    StartCoroutine(ServiceManager.Instance.ChatService.InsertSummary(
                        convoId, text,
                        onSuccess: () => Debug.Log($"{LogTag} [SUMMARY] saved for {convoId}"),
                        onError:   err => Debug.LogWarning($"{LogTag} [SUMMARY] save failed: {err}")
                    ));
                }
            },
            onError: err => Debug.LogWarning($"{LogTag} [SUMMARY] ({convoId}) ERROR: {err}")
        ));
    }

    #endregion

    #region UI Rendering

    private void RebuildChatUI(string convoId)
    {
        ClearChat();

        var list = _messageCache[convoId];
        for (int i = 0; i < list.Count; i++)
        {
            var m = list[i];

            if (m.Id == TypingPlaceholderId)
            {
                var go   = Instantiate(_aiBubblePrefab, _chatContentParent);
                var ctrl = go.GetComponent<ChatBubbleController>();
                StartCoroutine(AnimateTyping(ctrl));
                continue;
            }

            // Kombinasi Reasoning + Bot → satu bubble dengan blockquote
            if (m.Sender == ReasoningSender)
            {
                string reasoning = m.Text ?? "";

                string response = null;
                if (i + 1 < list.Count && list[i + 1].Sender == BotSender)
                {
                    response = list[i + 1].Text ?? "";
                    i++; // skip Bot berikutnya karena sudah digabung
                }

                var combined = BuildAgenticCombined(reasoning, response);
                CreateBubble(combined, false);
                continue;
            }

            // Pesan Bot yang tidak didahului Reasoning
            if (m.Sender == BotSender)
            {
                CreateBubble(m.Text, false);
                continue;
            }

            // Default: user
            bool isUser = (m.Sender == CurrentUserId);
            CreateBubble(m.Text, isUser);
        }
    }

    private void CreateBubble(string message, bool isUser)
    {
        var prefab = isUser ? _userBubblePrefab : _aiBubblePrefab;
        var go     = Instantiate(prefab, _chatContentParent);
        go.GetComponent<ChatBubbleController>()?.SetText(message);
    }

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
            onError:   err => Debug.LogError($"{LogTag} DeleteMessages failed: {err}")
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
            onError: err => Debug.LogError($"{LogTag} DeleteConversation failed: {err}")
        );
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

    #region Text helpers (blockquote look)

    // Meniru tampilan blockquote ala Markdown '>'
    // │ (pipe) diwarnai abu-abu, isi reasoning dibuat italic & warna lebih soft.
    private string ToQuoteBlock(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var lines = s.Replace("\r", "").Split('\n');
        for (int i = 0; i < lines.Length; i++)
            lines[i] = $"<color=#CBD5E1>│ </color><color=#94A3B8><i>{lines[i]}</i></color>";
        return string.Join("\n", lines);
    }

    // Header + blockquote reasoning (+ response normal jika ada)
    private string BuildAgenticCombined(string reasoning, string response)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(reasoning))
        {
            sb.AppendLine("<b>🧠 Reasoning</b>");
            sb.AppendLine(ToQuoteBlock(reasoning.Trim()));
        }
        if (!string.IsNullOrWhiteSpace(response))
        {
            if (sb.Length > 0) sb.AppendLine("\n");
            sb.AppendLine("<b>💬 Response</b>");
            sb.AppendLine(response.Trim());
        }
        return sb.ToString().Trim();
    }

    #endregion
}