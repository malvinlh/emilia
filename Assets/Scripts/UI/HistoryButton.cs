using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class HistoryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _labelText;
    [SerializeField] private GameObject _trashIcon;

    [Header("Display Clamp (UI-only)")]
    [Tooltip("Jumlah maksimal karakter yang ditampilkan di label history (UI saja).")]
    [SerializeField] private int _maxLabelChars = 35;

    [Tooltip("Suffix yang dipakai saat dipotong.")]
    [SerializeField] private string _ellipsis = "…";

    private string _conversationId;
    private string _fullLabel;    // teks asli (tidak terpotong)
    private string _pendingLabel; // buffer kalau GO belum aktif
    private ChatManager _chatManager;

    public string ConversationId => _conversationId;

    private void Awake()
    {
        _chatManager = FindObjectOfType<ChatManager>();
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClicked);

        if (_labelText == null)
            _labelText = GetComponentInChildren<TextMeshProUGUI>();

        if (_trashIcon == null)
            _trashIcon = transform.Find("DeleteButton")?.gameObject;

        if (_trashIcon != null)
            _trashIcon.SetActive(false);
    }

    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(_pendingLabel))
        {
            _fullLabel = _pendingLabel;
            ApplyDisplayClamp(_fullLabel);
        }
    }

    public void SetConversationId(string id) => _conversationId = id;

    public void SetLabel(string text)
    {
        _pendingLabel = text ?? string.Empty;
        _fullLabel = _pendingLabel;

        if (_labelText != null && isActiveAndEnabled)
            ApplyDisplayClamp(_fullLabel);
    }

    public string GetCurrentLabel()
    {
        if (!string.IsNullOrEmpty(_fullLabel)) return _fullLabel;
        if (!string.IsNullOrEmpty(_pendingLabel)) return _pendingLabel;
        return _labelText != null ? _labelText.text : string.Empty;
    }

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

    private void ApplyDisplayClamp(string fullText)
    {
        if (_labelText == null) return;
        _labelText.text = ClampByLength(fullText, _maxLabelChars, _ellipsis);
    }

    private static string ClampByLength(string input, int maxChars, string ellipsis)
    {
        if (string.IsNullOrEmpty(input) || maxChars <= 0) return string.Empty;
        if (input.Length <= maxChars) return input;
        return input.Substring(0, maxChars) + (string.IsNullOrEmpty(ellipsis) ? "" : ellipsis);
    }
}