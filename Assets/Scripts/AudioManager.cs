using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

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
        PlaySceneBGM(scene.name);
    }

    private void PlaySceneBGM(string sceneName)
    {
        Transform sceneAudio = transform.Find(sceneName);
        if (sceneAudio == null)
        {
            Debug.LogWarning($"No audio setup found for scene '{sceneName}'.");
            return;
        }

        // Stop semua audio dulu
        foreach (Transform child in transform)
        {
            foreach (var audio in child.GetComponentsInChildren<AudioSource>())
            {
                audio.Stop();
            }
        }

        AudioSource sfx = sceneAudio.Find("SFXSource")?.GetComponent<AudioSource>();
        AudioSource bgm = sceneAudio.Find("BGMSource")?.GetComponent<AudioSource>();

        if (sfx != null)
        {
            sfx.Play();

            if (bgm != null)
            {
                StartCoroutine(PlayBGMAfterSFX(sfx.clip.length, bgm));
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
}