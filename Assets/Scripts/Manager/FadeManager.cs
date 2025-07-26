using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    #region Singleton

    public static FadeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #endregion

    #region Inspector Fields

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup _fadeCanvas;
    [SerializeField] private float       _fadeDuration = 1f;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        // Start fully black, then fade in
        _fadeCanvas.alpha = 1f;
        StartCoroutine(FadeInCoroutine());
    }

    #endregion

    #region Public API

    /// <summary>
    /// Fades the screen to black over the given duration (or default).
    /// Blocks raycasts while fading out.
    /// </summary>
    public IEnumerator FadeOutCoroutine(float customDuration = -1f)
    {
        float duration = GetDuration(customDuration);
        _fadeCanvas.blocksRaycasts = true;
        yield return FadeRoutine(0f, 1f, duration);
    }

    /// <summary>
    /// Fades the screen in from black to transparent over the default duration.
    /// Unblocks raycasts when complete.
    /// </summary>
    public IEnumerator FadeInCoroutine()
    {
        yield return FadeRoutine(1f, 0f, _fadeDuration);
        _fadeCanvas.blocksRaycasts = false;
    }

    /// <summary>
    /// Instantly sets the screen to black and blocks raycasts.
    /// </summary>
    public void InstantBlack()
    {
        SetAlpha(1f, true);
    }

    /// <summary>
    /// Instantly clears the screen (transparent) and unblocks raycasts.
    /// </summary>
    public void InstantClear()
    {
        SetAlpha(0f, false);
    }

    #endregion

    #region Private Helpers

    private float GetDuration(float customDuration)
    {
        return customDuration > 0f ? customDuration : _fadeDuration;
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _fadeCanvas.alpha = to;
    }

    private void SetAlpha(float alpha, bool blockRaycasts)
    {
        _fadeCanvas.alpha = alpha;
        _fadeCanvas.blocksRaycasts = blockRaycasts;
    }

    #endregion
}