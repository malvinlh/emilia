using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text.RegularExpressions;

[RequireComponent(typeof(Button))]
public class HistoryButton : MonoBehaviour
{
    string conversationId;
    string userId;
    ChatManager chatManager;

    void Awake()
    {
        // ambil ChatManager & userId sekali
        chatManager = FindObjectOfType<ChatManager>();
        userId      = chatManager.CurrentUserId;

        // parse angka dari nama GO
        var match = Regex.Match(gameObject.name, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int idx))
            conversationId = $"cv{idx:00}";
        else
            conversationId = gameObject.name;  // fallback

        // daftar listener untuk Button.onClick
        GetComponent<Button>().onClick.AddListener(OnClicked);
    }

    void OnClicked()
    {
        Debug.Log($"Load convo {conversationId} for {userId}");
        chatManager.OnHistoryClicked(conversationId);
    }
}
