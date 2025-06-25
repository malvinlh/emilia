using UnityEngine;
using UnityEngine.UI;

public class DeleteChatsSetting : MonoBehaviour
{
    public Button audioPrevButton;
    public Button yesButton;
    public Button noButton;

    private string      currentUserId;

    void Start()
    {
        currentUserId = PlayerPrefs.GetString("Nickname", "");

        yesButton.onClick.AddListener(OnYesClicked);
        noButton .onClick.AddListener(OnNoClicked);
    }

    public void OnYesClicked()
    {
        // disable dialog UI dulu (opsional)
        audioPrevButton.interactable = false;

        StartCoroutine(
            ServiceManager.Instance.ChatService.DeleteAllChats(
                currentUserId,
                onSuccess: () =>
                {
                    gameObject.SetActive(false);
                },
                onError: err =>
                {
                    Debug.LogError("DeleteAllChats failed: " + err);
                    gameObject.SetActive(false);
                }
            )
        );
    }

    public void OnNoClicked()
    {
        gameObject.SetActive(false);
    }
}