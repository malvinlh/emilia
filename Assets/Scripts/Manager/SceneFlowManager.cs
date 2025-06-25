using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SceneFlowManager : MonoBehaviour
{
    public static SceneFlowManager Instance { get; private set; }

    [Header("Manual Scene List")]
    public List<SceneInfo> scenes = new List<SceneInfo>();

    [HideInInspector]
    public string currentSceneKey = "";

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

    private void Start()
    {
        UpdateCurrentSceneKey();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCurrentSceneKey();
        // AudioManager.Instance?.PlayBGMForSceneKey(currentSceneKey);
    }

    private void UpdateCurrentSceneKey()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        foreach (var info in scenes)
        {
            if (info.sceneName == activeSceneName)
            {
                currentSceneKey = info.sceneKey;
                break;
            }
        }
    }

    /// <summary>
    /// Load by key with custom fade-out duration (in seconds).
    /// Pass a negative value to use the default fadeDuration.
    /// </summary>
    public void LoadSceneByKey(string key, float fadeDurationOverride)
    {
        SceneInfo target = scenes.Find(s => s.sceneKey == key);
        if (target != null)
            StartCoroutine(TransitionScene(target, fadeDurationOverride));
        else
            Debug.LogError("Scene key not found: " + key);
    }

    /// <summary>
    /// Load by key using default fadeDuration.
    /// </summary>
    public void LoadSceneByKey(string key)
    {
        LoadSceneByKey(key, -1f);
    }

    public void LoadNextScene()
    {
        int currentIndex = scenes.FindIndex(s => s.sceneKey == currentSceneKey);
        if (currentIndex >= 0 && currentIndex + 1 < scenes.Count)
            LoadSceneByKey(scenes[currentIndex + 1].sceneKey);
        else
            Debug.LogWarning("No next scene found.");
    }

    public void LoadPreviousScene()
    {
        int currentIndex = scenes.FindIndex(s => s.sceneKey == currentSceneKey);
        if (currentIndex > 0)
            LoadSceneByKey(scenes[currentIndex - 1].sceneKey);
        else
            Debug.LogWarning("No previous scene found.");
    }

    /// <summary>
    /// Hook for UI Buttons (from Inspector).
    /// </summary>
    public void LoadSceneByKeyFromButton(string key)
    {
        LoadSceneByKey(key);
    }

    private IEnumerator TransitionScene(SceneInfo target, float customFadeDuration)
    {
        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeOut(customFadeDuration);

        yield return SceneManager.LoadSceneAsync(target.sceneName);

        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeIn();
    }
}