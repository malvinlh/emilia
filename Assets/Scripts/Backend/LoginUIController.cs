// LoginUIController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoginUIController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField fullNameInput;
    public TMP_InputField nicknameInput;
    public Button continueButton;
    public TextMeshProUGUI errorText;  // optional: assign a TMP text to show errors

    void Start()
    {
        // disable Continue until nickname is non–empty
        continueButton.interactable = false;
        if (errorText != null) errorText.text = "";

        nicknameInput.onValueChanged.AddListener(OnNicknameChanged);
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnNicknameChanged(string value)
    {
        bool valid = !string.IsNullOrWhiteSpace(value);
        continueButton.interactable = valid;
        if (valid && errorText != null)
            errorText.text = "";
    }

    void OnContinueClicked()
    {
        string fullName = fullNameInput.text.Trim();
        string nickname = nicknameInput.text.Trim();

        // call UserService via ServiceManager
        StartCoroutine(
            ServiceManager.Instance.UserService.UpsertUser(
                nickname,
                fullName,
                onSuccess: () => {
                    // save locally and move to Chat scene
                    PlayerPrefs.SetString("Nickname", nickname);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("Chat");
                },
                onError: err => {
                    if (errorText != null)
                        errorText.text = "Login gagal: " + err;
                    else
                        Debug.LogError("Login gagal: " + err);
                }
            )
        );
    }
}