using UnityEngine;

public class SceneButtonHandler : MonoBehaviour
{
    public string targetSceneKey;

    public void LoadTargetScene()
    {
        if (SceneFlowManager.Instance != null)
        {
            Debug.Log("Button clicked");
            SceneFlowManager.Instance.LoadSceneByKey(targetSceneKey);
        }
        else
        {
            Debug.LogWarning("SceneFlowManager is not available.");
        }
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}
