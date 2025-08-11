using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class HistoryButton : MonoBehaviour, IPointerEnterHandler
{
    #region Inspector Fields

    [Header("UI Elements")]
    [Tooltip("The text label inside your button prefab")]
    [SerializeField] private TextMeshProUGUI _labelText;

    [Tooltip("Trash icon to show on hover")]
    [SerializeField] private GameObject _trashIcon;

    #endregion

    #region Private State

    private string _conversationId;
    private string _pendingLabel;
    private ChatManager _chatManager;

    #endregion

    #region Public State
    
    public string ConversationId => _conversationId;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _chatManager = FindObjectOfType<ChatManager>();
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClicked);

        if (_labelText == null)
            _labelText = GetComponentInChildren<TextMeshProUGUI>();

        // Hide trash icon initially
        if (_trashIcon == null)
        {
            _trashIcon = transform.Find("DeleteButton")?.gameObject;
            _trashIcon.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // If label was set before activation, apply it now
        if (!string.IsNullOrEmpty(_pendingLabel))
            _labelText.text = _pendingLabel;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called by ChatManager immediately after Instantiate
    /// </summary>
    public void SetConversationId(string id) => _conversationId = id;

    /// <summary>
    /// Called by ChatManager to set the button label.
    /// May be invoked before this GameObject is Active.
    /// </summary>
    public void SetLabel(string text)
    {
        _pendingLabel = text;
        if (_labelText != null && isActiveAndEnabled)
            _labelText.text = _pendingLabel;
    }

    public string GetCurrentLabel()
    {
        if (_labelText != null && !string.IsNullOrEmpty(_labelText.text))
            return _labelText.text;
        return _pendingLabel;
    }

    #endregion

    #region Input Handlers

    private void OnClicked()
    {
        Debug.Log($"[HistoryButton] Loading convo '{_conversationId}'");
        _chatManager.OnHistoryClicked(_conversationId);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_trashIcon != null)
            _trashIcon.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_trashIcon != null)
            _trashIcon.SetActive(false);
    }

    #endregion
}