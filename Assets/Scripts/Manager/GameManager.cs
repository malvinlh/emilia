using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton

    public static GameManager Instance { get; private set; }

    #endregion

    #region Inspector Fields

    [Header("Manager Prefabs")]
    [SerializeField] private GameObject dbManagerPrefab;
    [SerializeField] private GameObject audioManagerPrefab;
    [SerializeField] private GameObject sceneFlowManagerPrefab;
    [SerializeField] private GameObject serviceManagerPrefab;
    [SerializeField] private GameObject fadeManagerPrefab;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (!TryInitSingleton()) return;
        InitializeManagers();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Sets up the singleton instance. Returns true if this object is the singleton owner.
    /// </summary>
    private bool TryInitSingleton()
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

    /// <summary>
    /// Ensures each core manager exists by instantiating its prefab if needed.
    /// </summary>
    private void InitializeManagers()
    {
        EnsureManager(EMILIA.Data.DatabaseManager.Instance,        dbManagerPrefab);
        EnsureManager(AudioManager.Instance,                       audioManagerPrefab);
        EnsureManager(SceneFlowManager.Instance,                   sceneFlowManagerPrefab);
        EnsureManager(ServiceManager.Instance,                     serviceManagerPrefab);
        EnsureManager(FadeManager.Instance,                        fadeManagerPrefab);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Instantiates <paramref name="prefab"/> if <paramref name="existing"/> is null.
    /// </summary>
    private static void EnsureManager(Object existing, GameObject prefab)
    {
        if (existing == null)
            Instantiate(prefab);
    }

    #endregion
}