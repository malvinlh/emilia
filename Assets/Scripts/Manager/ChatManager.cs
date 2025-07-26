// ChatManager.cs
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
    [SerializeField] private GameObject _userBubblePrefab;
    [SerializeField] private GameObject _aiBubblePrefab;
    [SerializeField] private GameObject _historyButtonPrefab;
    [SerializeField] private Transform _chatContentParent;
    [SerializeField] private Transform _chatHistoryParent;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private Button _newChatButton;
    [SerializeField] private Button _sendButton;

    [Header("Delete Confirmation UI")]
    [SerializeField] private GameObject _deleteChatSetting;
    [SerializeField] private Button _deleteYesButton;
    [SerializeField] private Button _deleteNoButton;

    #endregion

    #region Constants & Fields

    private const string PrefKeyNickname        = "Nickname";
    private const int    SnippetMaxLength       = 20;
    private static readonly Regex ConversationRegex =
        new Regex(@"cv(\d+)$", RegexOptions.Compiled);

    [HideInInspector]
    public string CurrentUserId;

    private string _currentConversationId;
    private string _pendingDeleteId;
    private bool   _isAwaitingResponse;
    private readonly List<string> _userConvs = new List<string>();

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

    #region Initialization

    private void LoadCurrentUser()
    {
        CurrentUserId = PlayerPrefs.GetString(PrefKeyNickname, "");
        Debug.Log($"[ChatManager] CurrentUserId = '{CurrentUserId}'");
        _currentConversationId = null;
        ClearChat();
    }

    private void InitializeUI()
    {
        _deleteChatSetting.SetActive(false);

        _newChatButton.onClick.AddListener(OnNewChatClicked);
        _sendButton.onClick.AddListener(OnSendClicked);
        _deleteNoButton.onClick.AddListener(() =>
        {
            _deleteChatSetting.SetActive(false);
            _pendingDeleteId = null;
        });
        _deleteYesButton.onClick.AddListener(ConfirmDeleteConversation);
    }

    #endregion

    #region History Management

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
        // clear existing buttons
        for (int i = _chatHistoryParent.childCount - 1; i >= 0; i--)
            Destroy(_chatHistoryParent.GetChild(i).gameObject);

        // create new ones
        for (int i = 0; i < convIds.Length; i++)
        {
            SetupHistoryButton(convIds[i], i);
        }
    }

    private void SetupHistoryButton(string convId, int index)
    {
        var go = Instantiate(_historyButtonPrefab, _chatHistoryParent);
        var hb = go.GetComponent<HistoryButton>();
        hb.SetConversationId(convId);
        hb.SetLabel($"Chat {index + 1}");

        // update snippet asynchronously
        StartCoroutine(ServiceManager.Instance.ChatService.FetchFirstMessage(
            convId,
            firstMsg =>
            {
                hb.SetLabel(FormatSnippet(firstMsg, index + 1));
            },
            err => Debug.LogWarning($"FetchFirstMessage failed for {convId}: {err}")
        ));

        // hook delete button
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
        StartCoroutine(ServiceManager.Instance.ChatService.FetchConversationWithMessages(
            conversationId,
            CurrentUserId,
            msgs =>
            {
                foreach (var m in msgs)
                    CreateBubble(m.Text, m.Sender == CurrentUserId);
            },
            err => Debug.LogError($"Fetch conversation failed: {err}")
        ));
    }

    #endregion

    #region Chat Sending

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
            StartCoroutine(SendUserMessage(text));
    }

    private void StartNewConversation(string text)
    {
        _currentConversationId = GenerateConversationId();
        _userConvs.Add(_currentConversationId);
        AddHistoryButtonForNew(_currentConversationId, text);

        StartCoroutine(ServiceManager.Instance.ChatService.CreateConversation(
            _currentConversationId,
            CurrentUserId,
            onSuccess: () => StartCoroutine(SendUserMessage(text)),
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
        hb.SetLabel(FormatSnippet(initialMsg, 1));

        var delBtn = go.transform.Find("DeleteButton")?.GetComponent<Button>();
        if (delBtn != null)
            delBtn.onClick.AddListener(() => OnDeleteClicked(convId));
    }

    private IEnumerator SendUserMessage(string text)
    {
        yield return ServiceManager.Instance.ChatService.InsertMessage(
            _currentConversationId,
            CurrentUserId,
            text,
            onSuccess: () => StartCoroutine(HandleAITurn(text)),
            onError:   err => Debug.LogError($"Insert message failed: {err}")
        );
    }

    #endregion

    #region AI Handling

    private IEnumerator HandleAITurn(string userMessage)
    {
        _isAwaitingResponse = true;

        var typingGO   = Instantiate(_aiBubblePrefab, _chatContentParent);
        var typingCtrl = typingGO.GetComponent<ChatBubbleController>();
        var anim       = StartCoroutine(AnimateTyping(typingCtrl));

        yield return OllamaService.SendPrompt(userMessage, response =>
        {
            StopCoroutine(anim);
            Destroy(typingGO);
            CreateBubble(response, false);
            SaveAIMessage(response);
            _isAwaitingResponse = false;
        });
    }

    private void SaveAIMessage(string response)
    {
        StartCoroutine(ServiceManager.Instance.ChatService.InsertMessage(
            _currentConversationId,
            "Bot",
            response,
            onSuccess: () => Debug.Log("✅ AI message saved"),
            onError:   err => Debug.LogError($"Save AI message failed: {err}")
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
        if (string.IsNullOrEmpty(_pendingDeleteId))
            return;

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

    #region Utilities

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