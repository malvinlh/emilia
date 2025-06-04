using UnityEngine;
using TMPro;
using UnityEngine.UI; // untuk LayoutElement

[RequireComponent(typeof(RectTransform))]
public class ChatBubbleController : MonoBehaviour
{
    [Header("Drag your TMP Text component here")]
    public TextMeshProUGUI chatText;

    [Tooltip("The maximum width (in pixels) that the chat text should occupy.")]
    public float maxTextWidth;

    // Kita menyimpan referensi LayoutElement sekali di Awake
    private LayoutElement le;

    void Awake()
    {
        if (chatText == null)
        {
            Debug.LogError("ChatBubbleController: chatText is not assigned.");
            return;
        }

        // 1) Paksa TMP wrap di batas kata
        chatText.textWrappingMode = TextWrappingModes.Normal;

        // 2) Biar TMP tumbuh vertikal, bukan truncate
        chatText.overflowMode = TextOverflowModes.Overflow;

        // 3) Dapatkan atau tambahkan LayoutElement sekali di sini
        le = chatText.GetComponent<LayoutElement>();
        if (le == null)
        {
            le = chatText.gameObject.AddComponent<LayoutElement>();
        }

        // Jangan set preferredWidth di sini—karena nilai ini berubah‐ubah saat teks di‐update
    }

    /// <summary>
    /// Assign teks baru, lalu hitung lebarnya sesuai isi (wrap jika melebihi maxTextWidth).
    /// </summary>
    public void SetText(string text)
    {
        if (chatText == null || le == null) return;

        // 1) Set teks di TMP
        chatText.text = text;

        // 2) Paksa TMP menghitung ulang mesh-nya (agar GetPreferredValues akurat)
        chatText.ForceMeshUpdate();

        // 3) Hitung preferred values (lebar ideal) berdasarkan konten
        Vector2 ukuranIdeal = chatText.GetPreferredValues(text);
        float widthIdeal = ukuranIdeal.x;

        // 4) Clamp agar tidak melebihi maxTextWidth
        float lebarAkhir = Mathf.Clamp(widthIdeal, 0f, maxTextWidth);

        // 5) Set LayoutElement.ke—sehingga sistem layout Unity men‐resize bubble
        le.preferredWidth = lebarAkhir;

        // (Optional) jika Anda ingin tinggi bubble mengikuti teks juga, 
        // bisa tambahkan: le.preferredHeight = chatText.GetPreferredValues(text).y;
        // Tapi seringkali ContentSizeFitter di atas TMP sudah cukup mengatur tinggi.
    }
}