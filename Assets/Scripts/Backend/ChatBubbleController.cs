using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ChatBubbleController : MonoBehaviour
{
    [Header("TMP Text")]
    public TextMeshProUGUI chatText;

    [Tooltip("Max width (px) for the text")]
    public float maxTextWidth = 400f;

    private LayoutElement le;

    void Awake()
    {
        if (chatText == null)
        {
            Debug.LogError("ChatBubbleController: chatText is not assigned.");
            return;
        }

        chatText.textWrappingMode = TextWrappingModes.Normal;
        chatText.overflowMode     = TextOverflowModes.Overflow;

        le = chatText.GetComponent<LayoutElement>();
        if (le == null)
            le = chatText.gameObject.AddComponent<LayoutElement>();
    }

    public void SetText(string text)
    {
        if (chatText == null || le == null) return;

        chatText.text = text;
        chatText.ForceMeshUpdate();

        Vector2 size = chatText.GetPreferredValues(text);
        float width  = Mathf.Clamp(size.x, 0f, maxTextWidth);
        le.preferredWidth = width;
    }
}
