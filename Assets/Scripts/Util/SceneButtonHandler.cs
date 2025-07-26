using UnityEngine;

public class SceneButtonHandler : MonoBehaviour
{
    #region Inspector Fields

    [Header("Scene Settings")]
    [SerializeField]
    private string _targetSceneKey;

    [Header("Optional Button SFX")]
    [Tooltip("Leave null if no click sound is needed")]
    [SerializeField]
    private AudioClip _buttonSfx;

    #endregion

    #region Public API

    /// <summary>
    /// Called by the UI Button to trigger a scene load (and optional SFX).
    /// </summary>
    public void LoadTargetScene()
    {
        if (!HasSceneFlowManager()) 
            return;

        if (TryPlayButtonSfx(out float sfxDuration))
            LoadSceneWithFade(sfxDuration);
        else
            LoadSceneWithFade(); // uses default fade duration
    }

    /// <summary>
    /// Called by the UI Button to quit the application.
    /// </summary>
    public void ExitApp()
    {
        Application.Quit();
    }

    #endregion

    #region Helpers

    private bool HasSceneFlowManager()
    {
        if (SceneFlowManager.Instance == null)
        {
            Debug.LogWarning("[SceneButtonHandler] SceneFlowManager is not available.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Plays the optional button SFX and returns its length to use as fade time.
    /// </summary>
    private bool TryPlayButtonSfx(out float duration)
    {
        duration = 0f;

        if (_buttonSfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_buttonSfx);
            duration = _buttonSfx.length;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Delegates to SceneFlowManager, with an override or default fade duration.
    /// </summary>
    private void LoadSceneWithFade(float overrideFade = -1f)
    {
        if (overrideFade > 0f)
            SceneFlowManager.Instance.LoadSceneByKey(_targetSceneKey, overrideFade);
        else
            SceneFlowManager.Instance.LoadSceneByKey(_targetSceneKey);
    }

    #endregion
}