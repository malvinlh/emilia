using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : MonoBehaviour
{
    #region Singleton

    public static SceneFlowManager Instance { get; private set; }

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

    [Header("Manual Scene List")]
    [SerializeField] private List<SceneInfo> _scenes = new List<SceneInfo>();

    [HideInInspector]
    [SerializeField] private string _currentSceneKey = "";

    /// <summary>
    /// Key of the currently active scene, updated on each load.
    /// </summary>
    public string CurrentSceneKey => _currentSceneKey;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        UpdateCurrentSceneKey();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    #endregion

    #region Public API

    /// <summary>
    /// Loads a scene by its key, using either a custom or default fade duration.
    /// </summary>
    public void LoadSceneByKey(string key, float fadeDurationOverride)
    {
        var target = _scenes.Find(s => s.sceneKey == key);
        if (target != null)
            StartCoroutine(TransitionScene(target, fadeDurationOverride));
        else
            Debug.LogError($"[SceneFlowManager] Scene key not found: {key}");
    }

    /// <summary>
    /// Loads a scene by its key, using the default fade duration.
    /// </summary>
    public void LoadSceneByKey(string key) =>
        LoadSceneByKey(key, -1f);

    /// <summary>
    /// Loads the next scene in the list, if any.
    /// </summary>
    public void LoadNextScene()
    {
        int idx = _scenes.FindIndex(s => s.sceneKey == _currentSceneKey);
        if (idx >= 0 && idx + 1 < _scenes.Count)
            LoadSceneByKey(_scenes[idx + 1].sceneKey);
        else
            Debug.LogWarning("[SceneFlowManager] No next scene found.");
    }

    /// <summary>
    /// Loads the previous scene in the list, if any.
    /// </summary>
    public void LoadPreviousScene()
    {
        int idx = _scenes.FindIndex(s => s.sceneKey == _currentSceneKey);
        if (idx > 0)
            LoadSceneByKey(_scenes[idx - 1].sceneKey);
        else
            Debug.LogWarning("[SceneFlowManager] No previous scene found.");
    }

    /// <summary>
    /// Hook for UI buttons in the Inspector.
    /// </summary>
    public void LoadSceneByKeyFromButton(string key) =>
        LoadSceneByKey(key);

    #endregion

    #region Private Helpers

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCurrentSceneKey();
        // AudioManager.Instance?.PlayBGMForSceneKey(_currentSceneKey);
    }

    private void UpdateCurrentSceneKey()
    {
        string activeName = SceneManager.GetActiveScene().name;
        foreach (var info in _scenes)
        {
            if (info.sceneName == activeName)
            {
                _currentSceneKey = info.sceneKey;
                return;
            }
        }
    }

    private IEnumerator TransitionScene(SceneInfo target, float customFadeDuration)
    {
        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeOutCoroutine(customFadeDuration);

        yield return SceneManager.LoadSceneAsync(target.sceneName);

        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeInCoroutine();
    }

    #endregion
}