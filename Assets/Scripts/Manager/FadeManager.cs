using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    public CanvasGroup fadeCanvas;
    public float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Mulai dari layar hitam, kemudian langsung fade-in
        fadeCanvas.alpha = 1f;
        StartCoroutine(FadeIn());
    }

    public IEnumerator FadeOut(float customDuration = -1f)
    {
        float duration = customDuration > 0f ? customDuration : fadeDuration;
        fadeCanvas.blocksRaycasts = true;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeCanvas.alpha = t / duration;
            yield return null;
        }
        fadeCanvas.alpha = 1f;
    }

    public IEnumerator FadeIn()
    {
        float t = fadeDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadeCanvas.alpha = t / fadeDuration;
            yield return null;
        }
        fadeCanvas.alpha = 0f;

        // Unblock klik setelah fade-in selesai
        fadeCanvas.blocksRaycasts = false;
    }

    public void InstantBlack()
    {
        fadeCanvas.alpha = 1f;
        fadeCanvas.blocksRaycasts = true;
    }

    public void InstantClear()
    {
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
    }
}