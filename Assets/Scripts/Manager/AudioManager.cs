using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    #region Singleton

    public static AudioManager Instance { get; private set; }

    #endregion

    #region Constants & Fields

    private const string PrefKeyMasterVolume = "MasterVolume";
    private const string SfxSourceName        = "SFXSource";
    private const string BgmSourceName        = "BGMSource";

    private Dictionary<string, Transform> _sceneAudioMap = new Dictionary<string, Transform>();
    private Coroutine                     _sfxDelayCoroutine;
    private AudioSource[]                 _sources;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (!InitializeSingleton())
            return;

        CacheAudioSources();
        RestoreMasterVolume();
        BuildSceneAudioMap();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Sets volume for all managed AudioSources and persists the value.
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        foreach (var src in _sources)
            src.volume = volume;

        PlayerPrefs.SetFloat(PrefKeyMasterVolume, volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Plays a one-shot SFX clip at the given volume and destroys it when done.
    /// </summary>
    public AudioSource PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return null;

        var go = new GameObject($"SFX_{clip.name}");
        var sfx = go.AddComponent<AudioSource>();
        sfx.clip   = clip;
        sfx.volume = volume;
        sfx.Play();

        Destroy(go, clip.length);
        return sfx;
    }

    #endregion

    #region Initialization Helpers

    private bool InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }

        Destroy(gameObject);
        return false;
    }

    private void CacheAudioSources()
    {
        _sources = GetComponentsInChildren<AudioSource>(true);
    }

    private void RestoreMasterVolume()
    {
        var savedVol = PlayerPrefs.GetFloat(PrefKeyMasterVolume, 1f);
        SetMasterVolume(savedVol);
    }

    private void BuildSceneAudioMap()
    {
        _sceneAudioMap.Clear();
        foreach (Transform child in transform)
            _sceneAudioMap[child.name] = child;
    }

    #endregion

    #region Scene BGM Logic

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        => PlaySceneBGM(scene.name);

    private void PlaySceneBGM(string sceneName)
    {
        if (!_sceneAudioMap.TryGetValue(sceneName, out var audioRoot))
        {
            Debug.LogWarning($"[AudioManager] No audio setup found for scene '{sceneName}'.");
            return;
        }

        StopAllSceneAudio();
        CancelPendingSfxDelay();

        var sfxTransform = audioRoot.Find(SfxSourceName);
        var bgmTransform = audioRoot.Find(BgmSourceName);

        AudioSource sfx = null;
        if (sfxTransform != null)
            sfxTransform.TryGetComponent(out sfx);

        AudioSource bgm = null;
        if (bgmTransform != null)
            bgmTransform.TryGetComponent(out bgm);

        if (sfx != null)
        {
            sfx.Play();
            if (bgm != null)
                _sfxDelayCoroutine = StartCoroutine(PlayBGMAfterSFX(sfx.clip.length, bgm));
        }
        else if (bgm != null)
        {
            bgm.Play();
        }
    }

    private void StopAllSceneAudio()
    {
        foreach (var kvp in _sceneAudioMap)
        foreach (var src in kvp.Value.GetComponentsInChildren<AudioSource>())
            src.Stop();
    }

    private void CancelPendingSfxDelay()
    {
        if (_sfxDelayCoroutine != null)
        {
            StopCoroutine(_sfxDelayCoroutine);
            _sfxDelayCoroutine = null;
        }
    }

    private IEnumerator PlayBGMAfterSFX(float delay, AudioSource bgm)
    {
        yield return new WaitForSeconds(delay);
        bgm.Play();
    }

    #endregion
}