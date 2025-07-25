// ServiceManager.cs
using UnityEngine;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }

    [HideInInspector] public LocalUserService    UserService;
    [HideInInspector] public LocalChatService    ChatService;
    [HideInInspector] public LocalJournalService JournalService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            UserService    = gameObject.AddComponent<LocalUserService>();
            ChatService    = gameObject.AddComponent<LocalChatService>();
            JournalService = gameObject.AddComponent<LocalJournalService>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}