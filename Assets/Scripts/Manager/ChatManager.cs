using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
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

    [Header("Reasoning (Agentic)")]
    [SerializeField] private Button         _reasoningSendButton; // Toggle ON
    [SerializeField] private Button         _reasoningStopButton; // Toggle OFF

    [Header("Audio Recording")]
    [SerializeField] private RecordAudio    _recorder;            // drag komponen RecordAudio di sini

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

    private const string BotSender       = "Bot";
    private const string ReasoningSender = "Reasoning";

    private static readonly Regex ConversationRegex =
        new Regex(@"cv(\d+)$", RegexOptions.Compiled);

    [HideInInspector] public string CurrentUserId;

    private string _currentConversationId;
    private string _pendingDeleteId;

    private bool   _isAwaitingResponse;
    private bool   _isReasoningMode = false; // toggle global

    private readonly Dictionary<string, List<Message>> _messageCache =
        new Dictionary<string, List<Message>>();

    private readonly HashSet<string> _isTyping = new HashSet<string>();
    private readonly List<string>    _userConvs = new List<string>();

    private readonly Dictionary<string, string> _topicCache     = new Dictionary<string, string>();
    private readonly HashSet<string>            _topicRequested = new HashSet<string>();
    private readonly Dictionary<string, int>    _lastSummarizedPairCount = new Dictionary<string, int>();

    [SerializeField] private Animator anim;

    #endregion

    #region Unity

    private void Awake()
    {
        LoadCurrentUser();
        InitializeUI();
    }

    private void Start()
    {
        FetchAndPopulateHistory();
    }

    private void OnEnable()
    {
        // subscribe event dari recorder (kalau ada)
        if (_recorder != null)
            _recorder.OnSaved += OnAudioSaved;
    }

    private void OnDisable()
    {
        if (_recorder != null)
            _recorder.OnSaved -= OnAudioSaved;
    }

    #endregion

    #region Init & Session

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

        _currentConversationId = null;
        ClearChat();
    }

    private void InitializeUI()
    {
        _deleteChatSetting.SetActive(false);

        _newChatButton?.onClick.AddListener(OnNewChatClicked);
        _sendButton    ?.onClick.AddListener(OnSendClicked);

        if (_reasoningSendButton != null) _reasoningSendButton.onClick.AddListener(EnterReasoningMode);
        if (_reasoningStopButton != null) _reasoningStopButton.onClick.AddListener(ExitReasoningMode);

        _isReasoningMode = false;
        ApplyReasoningVisibilityByMode();
        UpdateReasoningInteractable();

        _deleteNoButton?.onClick.AddListener(() =>
        {
            _deleteChatSetting.SetActive(false);
            _pendingDeleteId = null;
        });
        _deleteYesButton?.onClick.AddListener(ConfirmDeleteConversation);
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

        _isReasoningMode = false;
        ApplyReasoningVisibilityByMode();
        UpdateReasoningInteractable();

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

        _isReasoningMode = false;
        ApplyReasoningVisibilityByMode();
        UpdateReasoningInteractable();
    }

    #endregion

    #region Reasoning Toggle

    private void EnterReasoningMode()
    {
        _isReasoningMode = true;

        anim.SetBool("isThinking", true);
        ApplyReasoningVisibilityByMode(); // show/hide GO hanya saat toggle
        UpdateReasoningInteractable();
    }

    private void ExitReasoningMode()
    {
        _isReasoningMode = false;

        anim.SetBool("isThinking", false);
        ApplyReasoningVisibilityByMode();
        UpdateReasoningInteractable();
    }

    private void ApplyReasoningVisibilityByMode()
    {
        if (_reasoningSendButton != null)
            _reasoningSendButton.gameObject.SetActive(!_isReasoningMode);
        if (_reasoningStopButton != null)
            _reasoningStopButton.gameObject.SetActive(_isReasoningMode);
    }

    private void UpdateReasoningInteractable()
    {
        if (_reasoningSendButton != null)
            _reasoningSendButton.interactable = !_isAwaitingResponse && !_isReasoningMode;
        if (_reasoningStopButton != null)
            _reasoningStopButton.interactable = !_isAwaitingResponse &&  _isReasoningMode;
    }

    private void SetAwaiting(bool value)
    {
        _isAwaitingResponse = value;

        if (_sendButton    != null) _sendButton.interactable     = !value;
        if (_newChatButton != null) _newChatButton.interactable  = !value;
        if (_inputField    != null) _inputField.interactable     = !value;

        UpdateReasoningInteractable();
    }

    #endregion

    #region Audio → draft ke InputField

    /// <summary>
    /// Dipanggil dari RecordAudio saat file WAV sudah tersimpan.
    /// Kita upload ke endpoint sesuai mode, ambil teks, lalu masukin ke input field.
    /// </summary>
    private void OnAudioSaved(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Debug.LogWarning($"Audio file not found: {filePath}");
            return;
        }
        // Transcribe → hasil ke input field (draft)
        StartCoroutine(TranscribeAndFillInput(filePath));
    }

    private IEnumerator TranscribeAndFillInput(string filePath)
    {
        SetAwaiting(true); // kunci UI sementara (ikon reasoning tetap visible tapi non-interactable)
        yield return ServiceManager.Instance.TranscribeApi.TranscribeFile(
            filePath,
            onSuccess: text =>
            {
                _inputField.text = text ?? "";
                // fokuskan caret di akhir supaya siap diedit/Send
                _inputField.caretPosition = _inputField.text.Length;
                SetAwaiting(false);
            },
            onError: err =>
            {
                Debug.LogError($"Transcribe error: {err}");
                SetAwaiting(false);
            }
        );
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
                    var firstUser = msgs.FirstOrDefault(m => m.Sender != BotSender);
                    var firstBot  = msgs.FirstOrDefault(m => m.Sender == BotSender);
                    if (firstUser != null && firstBot != null)
                        TryGenerateTopicOnce(convId, firstUser.Text, firstBot.Text, hb);
                    else
                        hb.SetLabel(NewChatLabel);
                },
                err => Debug.LogWarning($"Fetch conv for topic failed: {err}")
            ));
        }

        var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null)
            delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
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
                        Sender         = BotSender,
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
        if (string.IsNullOrEmpty(text) || _isAwaitingResponse) return;

        _inputField.text = "";
        CreateBubble(text, true);

        if (string.IsNullOrEmpty(_currentConversationId))
        {
            if (_isReasoningMode)
                StartNewConversationForReasoning(text);
            else
                StartNewConversation(text);
        }
        else
        {
            if (_isReasoningMode)
                StartCoroutine(SendUserMessageReasoning(text, _currentConversationId));
            else
                StartCoroutine(SendUserMessage(text, _currentConversationId));
        }
    }

    // tombol reasoning send khusus (opsional)
    private void OnSendReasoningClicked()
    {
        var text = _inputField.text.Trim();
        if (string.IsNullOrEmpty(text) || _isAwaitingResponse) return;

        _inputField.text = "";
        CreateBubble(text, true);

        if (string.IsNullOrEmpty(_currentConversationId))
            StartNewConversationForReasoning(text);
        else
            StartCoroutine(SendUserMessageReasoning(text, _currentConversationId));
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

    private void StartNewConversationForReasoning(string text)
    {
        var convoId = GenerateConversationId();
        _currentConversationId = convoId;
        _userConvs.Add(convoId);
        _messageCache[convoId] = new List<Message>();

        AddHistoryButtonForNew(convoId, text);

        StartCoroutine(ServiceManager.Instance.ChatService.CreateConversation(
            convoId,
            CurrentUserId,
            onSuccess: () => StartCoroutine(SendUserMessageReasoning(text, convoId)),
            onError:   err => Debug.LogError($"CreateConversation (agentic) failed: {err}")
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
        hb.SetLabel(NewChatLabel);
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

    private IEnumerator SendUserMessageReasoning(string text, string convoId)
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
            onSuccess: () => StartCoroutine(HandleAgenticTurn(text, convoId)),
            onError:   err => Debug.LogError($"Insert message (agentic) failed: {err}")
        );
    }

    #endregion

    #region AI Handling & Typing

    private IEnumerator HandleAITurn(string userMessage, string convoId)
    {
        SetAwaiting(true);
        _isTyping.Add(convoId);

        _messageCache[convoId].Add(new Message {
            Id             = TypingPlaceholderId,
            ConversationId = convoId,
            Sender         = BotSender,
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
                    Sender         = BotSender,
                    Text           = response,
                    SentAt         = DateTime.UtcNow
                };
                _messageCache[convoId].Add(aiMsg);

                if (_currentConversationId == convoId)
                    RebuildChatUI(convoId);

                SaveAIMessage(response, convoId);
                SetAwaiting(false);

                TryGenerateTopicOnce(convoId, userMessage, response);
                TrySummarizeEveryTwoPairs(convoId);
            },
            onError: err =>
            {
                Debug.LogError($"Chat API error: {err}");
                _isTyping.Remove(convoId);
                _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);
                SetAwaiting(false);
                if (_currentConversationId == convoId)
                    RebuildChatUI(convoId);
            }
        );
    }

    private IEnumerator HandleAgenticTurn(string userMessage, string convoId)
    {
        SetAwaiting(true);
        _isTyping.Add(convoId);

        _messageCache[convoId].Add(new Message {
            Id             = TypingPlaceholderId,
            ConversationId = convoId,
            Sender         = BotSender,
            Text           = null,
            SentAt         = DateTime.UtcNow
        });

        if (_currentConversationId == convoId)
            RebuildChatUI(convoId);

        yield return ServiceManager.Instance.AgenticApi.Send(
            userId:    CurrentUserId,
            username:  CurrentUserId,
            question:  userMessage,
            onSuccess: res =>
            {
                _isTyping.Remove(convoId);
                _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);

                if (!string.IsNullOrWhiteSpace(res.reasoning))
                {
                    var rMsg = new Message {
                        Id             = Guid.NewGuid().ToString(),
                        ConversationId = convoId,
                        Sender         = ReasoningSender,
                        Text           = res.reasoning,
                        SentAt         = DateTime.UtcNow
                    };
                    _messageCache[convoId].Add(rMsg);
                    StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(convoId, ReasoningSender, rMsg.Text));
                }

                var botText = res.response ?? "";
                var bMsg = new Message {
                    Id             = Guid.NewGuid().ToString(),
                    ConversationId = convoId,
                    Sender         = BotSender,
                    Text           = botText,
                    SentAt         = DateTime.UtcNow
                };
                _messageCache[convoId].Add(bMsg);
                StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(convoId, BotSender, bMsg.Text));

                if (_currentConversationId == convoId)
                    RebuildChatUI(convoId);

                SetAwaiting(false);

                if (!string.IsNullOrWhiteSpace(res.summary))
                {
                    StartCoroutine(ServiceManager.Instance.ChatService.InsertSummary(
                        convoId,
                        res.summary,
                        onSuccess: () => Debug.Log($"[Agentic] summary saved for {convoId}"),
                        onError:   e => Debug.LogWarning($"[Agentic] save summary failed: {e}")
                    ));
                }

                TryGenerateTopicOnce(convoId, userMessage, botText);
                TrySummarizeEveryTwoPairs(convoId);
            },
            onError: err =>
            {
                Debug.LogError($"Agentic API error: {err}");
                _isTyping.Remove(convoId);
                _messageCache[convoId].RemoveAll(m => m.Id == TypingPlaceholderId);
                SetAwaiting(false);
                if (_currentConversationId == convoId)
                    RebuildChatUI(convoId);
            }
        );
    }

    private void SaveAIMessage(string response, string convoId)
    {
        StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(
            convoId,
            BotSender,
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

    #endregion

    #region Topic & Summary

    private void TryGenerateTopicOnce(string convoId, string userText, string botText, HistoryButton hb = null)
    {
        if (string.IsNullOrWhiteSpace(userText) || string.IsNullOrWhiteSpace(botText))
            return;

        var dbTitle = ServiceManager.Instance.ChatService.GetConversationTitle(convoId);
        if (!string.IsNullOrWhiteSpace(dbTitle))
        {
            _topicCache[convoId] = dbTitle;
            (hb ?? _chatHistoryParent.GetComponentsInChildren<HistoryButton>(true)
                                     .FirstOrDefault(h => h.ConversationId == convoId))
                                     ?.SetLabel(dbTitle);
            return;
        }

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
                    ServiceManager.Instance.ChatService.UpdateConversationTitle(convoId, topic);
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

    private void TrySummarizeEveryTwoPairs(string convoId)
    {
        if (!_messageCache.TryGetValue(convoId, out var msgs) || msgs == null) return;

        var ordered = msgs.OrderBy(m => m.SentAt).ToList();

        int pairs = 0;
        bool waitingForBot = false;

        foreach (var m in ordered)
        {
            if (m.Sender == BotSender)
            {
                if (waitingForBot) { pairs++; waitingForBot = false; }
            }
            else if (m.Sender != ReasoningSender)
            {
                waitingForBot = true;
            }
        }

        if (pairs < 2 || (pairs % 2 != 0)) return;

        int last = _lastSummarizedPairCount.TryGetValue(convoId, out var v) ? v : 0;
        if (pairs == last) return;

        _lastSummarizedPairCount[convoId] = pairs;

        if (ServiceManager.Instance?.SummaryApi == null) return;

        StartCoroutine(ServiceManager.Instance.SummaryApi.RequestSummary(
            convoId,
            onSuccess: text => Debug.Log($"[SUMMARY] ({convoId}) pairs={pairs}: {text}"),
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

            if (m.Sender == ReasoningSender)
            {
                string reasoning = m.Text ?? "";
                string response  = null;

                if (i + 1 < list.Count && list[i + 1].Sender == BotSender)
                {
                    response = list[i + 1].Text ?? "";
                    i++;
                }

                var go   = Instantiate(_aiBubblePrefab, _chatContentParent);
                var ctrl = go.GetComponent<ChatBubbleController>();
                ctrl.SetAgentic(reasoning, response);
                continue;
            }

            if (m.Sender == BotSender)
            {
                var go   = Instantiate(_aiBubblePrefab, _chatContentParent);
                var ctrl = go.GetComponent<ChatBubbleController>();
                ctrl.SetText(m.Text);
                continue;
            }

            bool isUser = (m.Sender == CurrentUserId);
            var g1   = Instantiate(isUser ? _userBubblePrefab : _aiBubblePrefab, _chatContentParent);
            var c1   = g1.GetComponent<ChatBubbleController>();
            c1.SetText(m.Text);
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