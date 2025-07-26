using UnityEngine;

public class ServiceManager : MonoBehaviour
{
    #region Singleton

    public static ServiceManager Instance { get; private set; }

    #endregion

    #region Services

    [HideInInspector] public LocalUserService    UserService    { get; private set; }
    [HideInInspector] public LocalChatService    ChatService    { get; private set; }
    [HideInInspector] public LocalJournalService JournalService { get; private set; }

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (!InitializeSingleton())
            return;

        InitializeServices();
    }

    #endregion

    #region Initialization

    private bool InitializeSingleton()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return false;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        return true;
    }

    private void InitializeServices()
    {
        UserService    = gameObject.AddComponent<LocalUserService>();
        ChatService    = gameObject.AddComponent<LocalChatService>();
        JournalService = gameObject.AddComponent<LocalJournalService>();
    }

    #endregion
}