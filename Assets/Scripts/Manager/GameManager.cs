using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameObject audioManagerPrefab;
    public GameObject sceneFlowManagerPrefab;
    public GameObject serviceManagerPrefab;
    public GameObject fadeManagerPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManagers();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeManagers()
    {
        if (AudioManager.Instance == null)
            Instantiate(audioManagerPrefab);

        if (SceneFlowManager.Instance == null)
            Instantiate(sceneFlowManagerPrefab);

        if (ServiceManager.Instance == null)
            Instantiate(serviceManagerPrefab);

        if (FadeManager.Instance == null)
            Instantiate(fadeManagerPrefab);
    }
}
