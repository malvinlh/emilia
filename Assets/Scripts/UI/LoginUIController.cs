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

    [Header("Error Labels (Separated)")]
    [SerializeField] private TextMeshProUGUI nicknameErrorText;
    [SerializeField] private TextMeshProUGUI fullNameErrorText;

    [SerializeField] private SceneButtonHandler sceneButtonHandler;

    #endregion

    #region Constants

    private const string NicknameRequiredMessage = "Nickname wajib diisi.";

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        ClearAllErrors();

        continueButton.onClick.AddListener(OnContinueClicked);

        // Hanya bersihkan error terkait saat field berubah
        nicknameInput.onValueChanged.AddListener(_ => ClearNicknameError());
        fullNameInput.onValueChanged.AddListener(_ => ClearFullNameError());
    }

    #endregion

    #region Event Handlers

    private void OnContinueClicked()
    {
        string nickname = nicknameInput.text.Trim();
        string fullName = fullNameInput.text.Trim();

        // Bersihkan error lama sebelum validasi baru
        ClearAllErrors();

        // Validasi lokal: nickname wajib
        if (string.IsNullOrWhiteSpace(nickname))
        {
            ShowNicknameError(NicknameRequiredMessage);
            return;
        }

        StartCoroutine(ServiceManager.Instance.UserService.UpsertUser(
            nickname,
            fullName,
            onSuccess: () => ProcessLoginSuccess(nickname),
            onError: err => HandleServiceError(err)
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

    #region Error Routing

    /// <summary>
    /// Meneruskan error dari service ke label yang tepat.
    /// Asumsi: error fullname mismatch mengandung kata "nama lengkap".
    /// Selain itu, tampilkan di label nickname (sebagai fallback umum).
    /// </summary>
    private void HandleServiceError(string message)
    {
        if (!string.IsNullOrEmpty(message) &&
            message.ToLower().Contains("nama lengkap"))
        {
            ShowFullNameError(message);
        }
        else
        {
            ShowNicknameError(message);
        }

        Debug.LogWarning($"[Login] Service error: {message}");
    }

    #endregion

    #region Helper Methods - Error UI

    private void ShowNicknameError(string message)
    {
        if (nicknameErrorText != null) nicknameErrorText.text = message;
        else Debug.LogWarning(message);
    }

    private void ShowFullNameError(string message)
    {
        if (fullNameErrorText != null) fullNameErrorText.text = message;
        else Debug.LogWarning(message);
    }

    private void ClearNicknameError()
    {
        if (nicknameErrorText != null) nicknameErrorText.text = string.Empty;
    }

    private void ClearFullNameError()
    {
        if (fullNameErrorText != null) fullNameErrorText.text = string.Empty;
    }

    private void ClearAllErrors()
    {
        ClearNicknameError();
        ClearFullNameError();
    }

    #endregion
}