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
    public TextMeshProUGUI errorText;
    public SceneButtonHandler sceneButtonHandler;

    void Start()
    {
        // kosongkan errorText
        if (errorText != null) 
            errorText.text = "";

        // selalu allow click, tapi kita validasi di OnContinueClicked
        continueButton.onClick.AddListener(OnContinueClicked);

        // optional: clear error saat user mulai ketik
        nicknameInput.onValueChanged.AddListener(_ => {
            if (!string.IsNullOrWhiteSpace(nicknameInput.text) && errorText != null)
                errorText.text = "";
        });
    }

    void OnContinueClicked()
    {
        string fullName = fullNameInput.text.Trim();
        string nickname = nicknameInput.text.Trim();

        // VALIDASI LOKAL
        if (string.IsNullOrWhiteSpace(nickname))
        {
            if (errorText != null)
                errorText.text = "⚠ Nickname wajib diisi!";
            return;
        }

        // baru panggil service
        StartCoroutine(
            ServiceManager.Instance.UserService.UpsertUser(
                nickname,
                fullName,
                onSuccess: () => {
                    PlayerPrefs.SetString("Nickname", nickname);
                    PlayerPrefs.Save();
                    Debug.Log($"[Login] Saved Nickname: {PlayerPrefs.GetString("Nickname")}");
                    sceneButtonHandler.LoadTargetScene();
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