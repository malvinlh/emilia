using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private Dictionary<string, Transform> sceneAudioMap = new Dictionary<string, Transform>();
    private Coroutine sfxDelayCoroutine = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildSceneAudioMap();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void BuildSceneAudioMap()
    {
        sceneAudioMap.Clear();
        foreach (Transform child in transform)
        {
            sceneAudioMap[child.name] = child;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneBGM(scene.name);
    }

    private void PlaySceneBGM(string sceneName)
    {
        if (!sceneAudioMap.TryGetValue(sceneName, out var sceneAudio))
        {
            Debug.LogWarning($"[AudioManager] No audio setup found for scene '{sceneName}'.");
            return;
        }

        // Stop semua audio dulu
        foreach (var kvp in sceneAudioMap)
        {
            foreach (var audio in kvp.Value.GetComponentsInChildren<AudioSource>())
            {
                audio.Stop();
            }
        }

        // Stop coroutine sebelumnya kalau ada
        if (sfxDelayCoroutine != null)
        {
            StopCoroutine(sfxDelayCoroutine);
            sfxDelayCoroutine = null;
        }

        AudioSource sfx = sceneAudio.Find("SFXSource")?.GetComponent<AudioSource>();
        AudioSource bgm = sceneAudio.Find("BGMSource")?.GetComponent<AudioSource>();

        if (sfx != null)
        {
            sfx.Play();

            if (bgm != null)
            {
                sfxDelayCoroutine = StartCoroutine(PlayBGMAfterSFX(sfx.clip.length, bgm));
            }
        }
        else if (bgm != null)
        {
            bgm.Play(); // kalau nggak ada SFX, langsung mainkan BGM
        }
    }

    private IEnumerator PlayBGMAfterSFX(float delay, AudioSource bgm)
    {
        yield return new WaitForSeconds(delay);
        bgm.Play();
    }

    public AudioSource PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return null;

        GameObject sfxGO = new GameObject("SFX_" + clip.name);
        AudioSource sfxSource = sfxGO.AddComponent<AudioSource>();
        sfxSource.clip = clip;
        sfxSource.volume = volume;
        sfxSource.Play();

        Destroy(sfxGO, clip.length); // Hapus setelah selesai
        return sfxSource;
    }
}