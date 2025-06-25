using UnityEngine;

public class SceneButtonHandler : MonoBehaviour
{
    public string targetSceneKey;
    [Tooltip("Leave null in scenes that don't need a button SFX")]
    public AudioClip buttonSfx;

    public void LoadTargetScene()
    {
        if (SceneFlowManager.Instance == null)
        {
            Debug.LogWarning("SceneFlowManager is not available.");
            return;
        }

        // Jika ada SFX, mainkan dulu dan gunakan durasinya sebagai fade
        if (buttonSfx != null && AudioManager.Instance != null)
        {
            AudioSource sfxSource = AudioManager.Instance.PlaySFX(buttonSfx);
            float fadeTime = buttonSfx.length;
            SceneFlowManager.Instance.LoadSceneByKey(targetSceneKey, fadeTime);
        }
        else
        {
            // Tanpa SFX: pakai fadeDuration default
            SceneFlowManager.Instance.LoadSceneByKey(targetSceneKey);
        }
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}