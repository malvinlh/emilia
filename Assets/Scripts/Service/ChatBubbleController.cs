// ChatBubbleController.cs
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ChatBubbleController : MonoBehaviour
{
    [Header("TMP Text")]
    public TextMeshProUGUI chatText;
    [Tooltip("Max width (px) for the text")]
    public float maxTextWidth = 400f;
    public float extraWidth = 20f;
    private LayoutElement le;

    void Awake()
    {
        if (chatText == null)
            chatText = GetComponentInChildren<TextMeshProUGUI>();

        // Aktifkan rich text dan atur wrap/overflow
        chatText.richText = true;
        chatText.textWrappingMode = TextWrappingModes.Normal;
        chatText.overflowMode     = TextOverflowModes.Overflow;

        le = chatText.GetComponent<LayoutElement>();
        if (le == null)
            le = chatText.gameObject.AddComponent<LayoutElement>();
    }

    /// <summary>
    /// Pasang teks user/AI, konversi Markdown → TMP tags, lalu resize bubble.
    /// </summary>
    public void SetText(string text)
    {
        if (chatText == null || le == null) return;

        string parsed = ParseMarkdownToTMP(text);

        chatText.text = parsed;
        chatText.ForceMeshUpdate();

        Vector2 size = chatText.GetPreferredValues(parsed);
        float targetW = size.x + extraWidth;
        targetW = Mathf.Clamp(targetW, 0f, maxTextWidth);
        le.preferredWidth = targetW;    
    }

    /// <summary>
    /// Ganti sintaks Markdown populer ke tag TextMeshPro.
    /// Mendukung:
    ///  - Heading #…######  → <size=…>
    ///  - **bold**, *italic*, _italic_, __underline__, ~~strike~~
    ///  - `inline code`, ```block code```
    ///  - [link](https://…) → <link>
    ///  - > quote → indent+italic
    ///  - - bullet list
    /// </summary>
    private string ParseMarkdownToTMP(string input)
    {
        string output = input;

        // 1) Code block ```lang\ncode```
        output = Regex.Replace(output, @"```(?:\w+)?\n([\s\S]+?)```", m =>
        {
            string code = m.Groups[1].Value
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
            return $"<noparse><font=\"Courier New\">{code}</font></noparse>";
        });

        // 2) Inline code `code`
        output = Regex.Replace(output, @"`(.+?)`", m =>
        {
            string code = m.Groups[1].Value
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
            return $"<noparse><font=\"Courier New\">{code}</font></noparse>";
        });

        // 3) Headings # … ######
        output = Regex.Replace(output, @"^###### (.+)$", "<size=24>$1</size>", RegexOptions.Multiline);
        output = Regex.Replace(output, @"^##### (.+)$",  "<size=26>$1</size>", RegexOptions.Multiline);
        output = Regex.Replace(output, @"^#### (.+)$",   "<size=28>$1</size>", RegexOptions.Multiline);
        output = Regex.Replace(output, @"^### (.+)$",    "<size=30>$1</size>", RegexOptions.Multiline);
        output = Regex.Replace(output, @"^## (.+)$",     "<size=32>$1</size>", RegexOptions.Multiline);
        output = Regex.Replace(output, @"^# (.+)$",      "<size=34>$1</size>", RegexOptions.Multiline);

        // 4) Formatting dasar
        output = Regex.Replace(output, @"\*\*(.+?)\*\*",   "<b>$1</b>");   // bold
        output = Regex.Replace(output, @"__(.+?)__",       "<u>$1</u>");   // underline
        output = Regex.Replace(output, @"~~(.+?)~~",       "<s>$1</s>");   // strikethrough
        output = Regex.Replace(output, @"\*(.+?)\*",       "<i>$1</i>");   // italic
        output = Regex.Replace(output, @"_(.+?)_",         "<i>$1</i>");   // italic

        // 5) Links [text](url)
        output = Regex.Replace(output,
            @"\[(.+?)\]\((https?:\/\/[^\s]+?)\)",
            "<link=\"$2\"><color=#0000EE><u>$1</u></color></link>");

        // 6) Blockquote > text
        output = Regex.Replace(output,
            @"^> (.+)$",
            "<indent=20%><i>$1</i></indent>",
            RegexOptions.Multiline);

        // 7) Bullet list - item / * item
        output = Regex.Replace(output,
            @"^[-\*] (.+)$",
            "• $1",
            RegexOptions.Multiline);

        return output;
    }
}