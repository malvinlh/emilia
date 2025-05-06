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

    public void LoadSceneByKey(string key)
    {
        SceneInfo target = scenes.Find(s => s.sceneKey == key);
        if (target != null)
            StartCoroutine(TransitionScene(target));
        else
            Debug.LogError("Scene key not found: " + key);
    }

    public void LoadNextScene()
    {
        int currentIndex = scenes.FindIndex(s => s.sceneKey == currentSceneKey);
        if (currentIndex >= 0 && currentIndex + 1 < scenes.Count)
            StartCoroutine(TransitionScene(scenes[currentIndex + 1]));
        else
            Debug.LogWarning("No next scene found.");
    }

    public void LoadPreviousScene()
    {
        int currentIndex = scenes.FindIndex(s => s.sceneKey == currentSceneKey);
        if (currentIndex > 0)
            StartCoroutine(TransitionScene(scenes[currentIndex - 1]));
        else
            Debug.LogWarning("No previous scene found.");
    }

    public void LoadSceneByKeyFromButton(string key)
    {
        LoadSceneByKey(key);
    }

    private IEnumerator TransitionScene(SceneInfo target)
    {
        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeOut();

        yield return SceneManager.LoadSceneAsync(target.sceneName);

        if (FadeManager.Instance != null)
            yield return FadeManager.Instance.FadeIn();
    }
}
