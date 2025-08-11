// ChatBubbleController.cs
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class ChatBubbleController : MonoBehaviour
{
    // ============================================================
    // NON-AGENTIC (user bubble & AI typing)
    // ============================================================
    [Header("TMP Text (Non-Agentic)")]
    public TextMeshProUGUI chatText;
    [Tooltip("Max width (px) for the text")]
    public float maxTextWidth = 400f;
    public float extraWidth   = 20f;

    // LayoutElement KHUSUS non-agentic → Nempel di chatText
    private LayoutElement le;

    // ============================================================
    // AGENTIC (reasoning + response dalam satu bubble)
    // ============================================================
    [Header("Quote Panel (Agentic)")]
    [Tooltip("Panel berisi garis kiri + teks reasoning (opsional)")]
    public GameObject       quotePanel;   // container reasoning
    public TextMeshProUGUI  quoteText;    // TMP di dalam quotePanel
    public Image            quoteBar;     // garis vertikal (opsional)
    [Tooltip("Tambahan gutter kiri untuk area quote (bar + padding)")]
    public float            quoteLeftGutter = 20f;

    [Header("Agentic Width")]
    [Tooltip("Max width saat mode agentic")]
    public float agenticMaxWidth = 400f;
    public float agenticExtraWidth = 20f;

    // LayoutElement KHUSUS agentic → Nempel di ROOT bubble
    private LayoutElement rootLEForAgentic;

    private void Awake()
    {
        // ====== NON-AGENTIC refs (biarkan persis versimu) ======
        if (chatText == null)
            chatText = GetComponentInChildren<TextMeshProUGUI>();

        chatText.richText         = true;
        chatText.textWrappingMode = TextWrappingModes.Normal;
        chatText.overflowMode     = TextOverflowModes.Overflow;

        le = chatText.GetComponent<LayoutElement>();
        if (le == null)
            le = chatText.gameObject.AddComponent<LayoutElement>();

        // ====== AGENTIC refs (optional, tidak mengganggu non-agentic) ======
        if (quotePanel == null)
            quotePanel = transform.Find("QuotePanel")?.gameObject;
        if (quoteText == null && quotePanel != null)
            quoteText = quotePanel.GetComponentInChildren<TextMeshProUGUI>(true);
        if (quoteBar == null && quotePanel != null)
            quoteBar = quotePanel.GetComponentInChildren<Image>(true);

        if (quoteText != null)
        {
            quoteText.richText         = true;
            quoteText.textWrappingMode = TextWrappingModes.Normal;
            quoteText.overflowMode     = TextOverflowModes.Overflow;
        }

        // JANGAN buat rootLEForAgentic di sini, supaya non-agentic tidak terpengaruh.
        // Nanti baru dibuat saat SetAgentic dipanggil.

        // Default: sembunyikan quotePanel
        if (quotePanel != null) quotePanel.SetActive(false);
    }

    // ============================================================
    // PUBLIC API — NON-AGENTIC
    // ============================================================

    /// <summary>
    /// Pasang teks user/AI, konversi Markdown → TMP tags, lalu resize bubble.
    /// (PERHITUNGAN NON-AGENTIC — DIBIARKAN SAMA)
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

    // ============================================================
    // PUBLIC API — AGENTIC
    // ============================================================

    /// <summary>
    /// Tampilkan Reasoning (di QuotePanel) + Response (di body) dalam satu bubble.
    /// Jalur hitung terpisah: pakai LayoutElement di ROOT, bukan di chatText.
    /// </summary>
    public void SetAgentic(string reasoning, string response)
    {
        // Kalau referensi body tidak ada, fallback sederhana
        if (chatText == null)
        {
            SetText(BuildAgenticFallback(reasoning, response));
            return;
        }

        // Aktifkan/Nonaktifkan panel quote
        bool hasReason = !string.IsNullOrWhiteSpace(reasoning);
        if (quotePanel != null) quotePanel.SetActive(hasReason);

        // Siapkan LayoutElement di ROOT khusus agentic (tidak mengganggu non-agentic)
        if (rootLEForAgentic == null)
        {
            rootLEForAgentic = GetComponent<LayoutElement>();
            if (rootLEForAgentic == null)
                rootLEForAgentic = gameObject.AddComponent<LayoutElement>();
        }

        // 1) Reasoning
        float quoteWidth = 0f;
        if (hasReason && quoteText != null)
        {
            string r = StyleQuote(ParseMarkdownToTMP(reasoning.Trim()));
            quoteText.text = r;
            quoteText.ForceMeshUpdate();

            // ukur lebar teks reasoning
            var rSize = quoteText.GetPreferredValues(r);
            quoteWidth = rSize.x + quoteLeftGutter; // bar + padding kiri
        }

        // 2) Response (body)
        string body = ParseMarkdownToTMP((response ?? string.Empty).Trim());
        chatText.text = body;
        chatText.ForceMeshUpdate();
        var bSize = chatText.GetPreferredValues(body);

        // 3) Lebar bubble (agentic) = max(quote, body) + padding agentic
        float contentW = Mathf.Max(bSize.x, quoteWidth);
        float targetW  = Mathf.Clamp(contentW + agenticExtraWidth, 0f, agenticMaxWidth);
        rootLEForAgentic.preferredWidth = targetW;
    }

    // ============================================================
    // HELPERS (dipakai keduanya)
    // ============================================================

    // Styling reasoning: warna soft + italic
    private string StyleQuote(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return $"<color=#94A3B8><i>{s}</i></color>";
    }

    // Fallback jika QuotePanel tidak ada
    private string BuildAgenticFallback(string reasoning, string response)
    {
        string quote = string.IsNullOrWhiteSpace(reasoning) ? "" : $"> {reasoning.Trim()}";
        string body  = string.IsNullOrWhiteSpace(response)  ? "" : response.Trim();
        if (string.IsNullOrEmpty(quote)) return body;
        return $"🧠 Reasoning\n{quote}\n\n💬 Response\n{body}";
    }

    /// <summary>
    /// Ganti sintaks Markdown populer ke tag TextMeshPro.
    /// (DIPAKAI BAIK OLEH NON-AGENTIC MAUPUN AGENTIC)
    /// </summary>
    private string ParseMarkdownToTMP(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

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
        output = Regex.Replace(output, @"\*\*(.+?)\*\*",   "<b>$1</b>");
        output = Regex.Replace(output, @"__(.+?)__",       "<u>$1</u>");
        output = Regex.Replace(output, @"~~(.+?)~~",       "<s>$1</s>");
        output = Regex.Replace(output, @"\*(.+?)\*",       "<i>$1</i>");
        output = Regex.Replace(output, @"_(.+?)_",         "<i>$1</i>");

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