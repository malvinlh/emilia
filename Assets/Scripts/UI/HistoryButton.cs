using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class HistoryButton : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Drag here the TMP Text element inside your button prefab")]
    [SerializeField] private TextMeshProUGUI labelText;

    private string conversationId;
    private string pendingLabel;      // 存放要显示的文字
    private ChatManager chatManager;

    private void Awake()
    {
        // 1) Get ChatManager instance
        chatManager = FindObjectOfType<ChatManager>();

        // 2) Hook click listener
        GetComponent<Button>().onClick.AddListener(OnClicked);

        // 3) Auto-find labelText if not assigned
        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Called by ChatManager immediately after Instantiate
    /// </summary>
    public void SetConversationId(string id)
    {
        conversationId = id;
    }

    /// <summary>
    /// Called by ChatManager to set the button label.
    /// May be invoked before this GameObject is Active.
    /// </summary>
    public void SetLabel(string text)
    {
        pendingLabel = text;

        // 如果此时已经是 Active，就马上更新
        if (labelText != null && isActiveAndEnabled)
            labelText.text = pendingLabel;
    }

    private void OnEnable()
    {
        // 每次从 Inactive → Active，都把 pendingLabel 写给 UI
        if (labelText != null && !string.IsNullOrEmpty(pendingLabel))
            labelText.text = pendingLabel;
    }

    private void OnClicked()
    {
        Debug.Log($"[HistoryButton] Clicked, will load convo '{conversationId}'");
        chatManager.OnHistoryClicked(conversationId);
    }
}