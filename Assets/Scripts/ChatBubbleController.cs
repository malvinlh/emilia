using UnityEngine;
using TMPro;

public class ChatBubbleController : MonoBehaviour
{
    public TextMeshProUGUI chatText;

    public void SetText(string text)
    {
        chatText.text = text;
    }
}
