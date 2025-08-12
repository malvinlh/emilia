using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoScrollToBottom : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    public void ScrollToBottom()
    {
        // Gunakan coroutine supaya dijalankan setelah layout selesai update
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null; // tunggu 1 frame
        scrollRect.verticalNormalizedPosition = 0f; // 0 = bawah, 1 = atas
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content); // pastikan layout terupdate
    }
}