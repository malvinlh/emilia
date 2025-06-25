using UnityEngine;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }

    [HideInInspector] public UserService UserService;
    [HideInInspector] public ChatService ChatService;
    [HideInInspector] public JournalService JournalService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            UserService = gameObject.AddComponent<UserService>();
            ChatService = gameObject.AddComponent<ChatService>();
            JournalService = gameObject.AddComponent<JournalService>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}