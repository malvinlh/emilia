using UnityEngine;

public class ServiceManager : MonoBehaviour
{
    public static ServiceManager Instance { get; private set; }

    [HideInInspector] public UserService UserService;
    [HideInInspector] public ChatService ChatService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Pastikan komponen ada
            UserService = gameObject.AddComponent<UserService>();
            ChatService = gameObject.AddComponent<ChatService>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}