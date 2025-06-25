using UnityEngine;

[System.Serializable]
public class SceneInfo
{
    public string sceneKey;    // ex: "MainMenu", "Level1"
    public string sceneName;   // must match name in Build Settings
    // public AudioClip bgmClip;  // assign BGM for this scene
}
