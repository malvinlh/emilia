using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class HistoryButton : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Drag here the TMP Text element inside your button prefab")]
    [SerializeField] private TextMeshProUGUI labelText;

    // Ini akan di‐set langsung oleh ChatManager.PopulateHistoryButtons(...)
    private string conversationId;
    private ChatManager chatManager;

    private void Awake()
    {
        // Ambil ChatManager instance
        chatManager = FindObjectOfType<ChatManager>();

        // Hook click listener
        GetComponent<Button>().onClick.AddListener(OnClicked);

        // Auto‐find labelText kalau belum di‐assign
        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void OnClicked()
    {
        Debug.Log($"[HistoryButton] Clicked, will load convo '{conversationId}'");
        chatManager.OnHistoryClicked(conversationId);
    }

    /// <summary>
    /// Dipanggil oleh ChatManager setelah Instantiate tombol.
    /// </summary>
    public void SetConversationId(string id)
    {
        conversationId = id;
    }

    /// <summary>
    /// Optional: set teks yang tampil di tombol (misal snippet atau "Chat 1").
    /// </summary>
    public void SetLabel(string text)
    {
        if (labelText != null)
            labelText.text = text;
    }
}
