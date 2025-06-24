using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // kalau mau pindah scene

public class LoginUIController : MonoBehaviour
{
    [Header("References")]
    public TMP_InputField fullNameInput;
    public TMP_InputField nicknameInput;
    public Button continueButton;
    public TextMeshProUGUI errorText; // optional, untuk menampilkan error

    void Start()
    {
        // awalnya tombol disable
        continueButton.interactable = false;
        errorText.text = "";

        // subscribe ke perubahan text nickname
        nicknameInput.onValueChanged.AddListener(OnNicknameChanged);

        // tombol dijalankan saat diklik
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    void OnNicknameChanged(string value)
    {
        // trim spasi, lalu cek non-empty
        bool valid = !string.IsNullOrWhiteSpace(value);
        continueButton.interactable = valid;

        // sembunyikan pesan jika sudah valid
        if (valid && errorText != null)
            errorText.text = "";
    }

    void OnContinueClicked()
    {
        string fullName = fullNameInput.text.Trim();
        string nickname = nicknameInput.text.Trim();

        if (string.IsNullOrEmpty(nickname))
        {
            // should not happen jika tombol sudah disable, tapi antisipasi
            errorText.text = "(Nickname wajib diisi)";
            return;
        }

        // Simpan data (misal PlayerPrefs atau model aplikasi)
        PlayerPrefs.SetString("FullName", fullName);
        PlayerPrefs.SetString("Nickname", nickname);
        PlayerPrefs.Save();

        // Lanjut ke scene berikutnya
        SceneManager.LoadScene("MainMenu");
        Debug.Log($"FullName: {fullName}, Nickname: {nickname}");
    }
}