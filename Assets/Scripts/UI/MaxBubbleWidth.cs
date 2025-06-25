using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(LayoutElement))]
public class MaxBubbleWidth : MonoBehaviour
{
    public TextMeshProUGUI chatText; // Assign the Text (child) here
    public float maxWidth = 500f; // Max width in pixels

    private LayoutElement layout;

    void Awake()
    {
        layout = GetComponent<LayoutElement>();
    }

    void Update()
    {
        if (chatText == null) return;

        // Calculate preferred width of the text
        float textPreferredWidth = chatText.preferredWidth;

        // Clamp width
        float targetWidth = Mathf.Min(textPreferredWidth, maxWidth);

        layout.preferredWidth = targetWidth;

        // Let height expand naturally (optional)
        layout.flexibleHeight = 1;
    }
}
