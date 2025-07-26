using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginUIController : MonoBehaviour
{
    #region Inspector Fields

    [Header("UI References")]
    [SerializeField] private TMP_InputField fullNameInput;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private SceneButtonHandler sceneButtonHandler;

    #endregion

    #region Constants

    private const string NicknameRequiredMessage = "⚠ Nickname wajib diisi.";
    private const string ErrorPrefix = "⚠ ";

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        ClearError();
        continueButton.onClick.AddListener(OnContinueClicked);
        nicknameInput.onValueChanged.AddListener(_ => ClearError());
        fullNameInput.onValueChanged.AddListener(_ => ClearError());
    }

    #endregion

    #region Event Handlers

    private void OnContinueClicked()
    {
        string nickname = nicknameInput.text.Trim();
        string fullName = fullNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(nickname))
        {
            ShowError(NicknameRequiredMessage);
            return;
        }

        StartCoroutine(ServiceManager.Instance.UserService.UpsertUser(
            nickname,
            fullName,
            onSuccess: () => ProcessLoginSuccess(nickname),
            onError: err => ShowError(ErrorPrefix + err)
        ));
    }

    private void ProcessLoginSuccess(string nickname)
    {
        PlayerPrefs.SetString("Nickname", nickname);
        PlayerPrefs.Save();
        Debug.Log($"[Login] Saved Nickname: {nickname}");
        sceneButtonHandler.LoadTargetScene();
    }

    #endregion

    #region Helper Methods

    private void ShowError(string message)
    {
        if (errorText != null)
            errorText.text = message;
        else
            Debug.LogWarning(message);
    }

    private void ClearError()
    {
        if (errorText != null)
            errorText.text = string.Empty;
    }

    #endregion
}