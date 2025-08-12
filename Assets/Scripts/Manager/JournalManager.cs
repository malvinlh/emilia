using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EMILIA.Data;

public class JournalManager : MonoBehaviour
{
    #region Inspector

    [Header("Home (List)")]
    [SerializeField] private GameObject journalEntryPrefab;  // Prefab item list
    [SerializeField] private Transform  homeContentParent;   // HomeCanvas/Viewport/Content
    [SerializeField] private Button     newJournalButton;    // Tombol "New" di Home
    [SerializeField] private GameObject bgNoNotes;           // BG-NoNotes (aktif saat list kosong)

    [Header("Canvases")]
    [SerializeField] private GameObject homeCanvas;
    [SerializeField] private GameObject journalCanvas;       // Dipakai untuk Create & Edit
    [SerializeField] private GameObject deleteCanvas;

    [Header("JournalCanvas (Create & Edit)")]
    [SerializeField] private TextMeshProUGUI headerText;     // "Create Journal" / "Edit Journal" (opsional)
    [SerializeField] private TMP_InputField  titleInput;     // Title input
    [SerializeField] private TMP_InputField  contentInput;   // Content input (TMP)
    [SerializeField] private Button          saveButton;     // Save
    [SerializeField] private Button          backButton;     // Back ke Home (opsional)

    [Header("DeleteCanvas")]
    [SerializeField] private TextMeshProUGUI deleteTitleText; // Teks konfirmasi (opsional)
    [SerializeField] private Button          deleteYesButton;
    [SerializeField] private Button          deleteNoButton;

    [Header("Validation (Optional)")]
    [SerializeField] private TextMeshProUGUI validationLabel; // tempat pesan error

    #endregion

    #region Fields

    private const string PrefKeyNickname = "Nickname";

    private enum JournalMode { Create, Edit }
    private JournalMode _mode;

    private Journal _currentJournal;        // konteks edit
    private Journal _journalPendingDelete;  // konteks delete

    #endregion

    #region Unity

    private void Start()
    {
        InitCanvases();
        BindUI();
        FetchUserJournals();
    }

    #endregion

    #region Init & Helpers

    private void InitCanvases()
    {
        ShowOnly(homeCanvas);
        if (bgNoNotes != null) bgNoNotes.SetActive(true); // default tampil saat awal
    }

    private void BindUI()
    {
        if (newJournalButton != null)
            newJournalButton.onClick.AddListener(OpenCreate);

        if (backButton != null)
            backButton.onClick.AddListener(() => ShowOnly(homeCanvas));

        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(OnSaveClicked);
        }

        if (deleteNoButton != null)
            deleteNoButton.onClick.AddListener(() => ShowOnly(homeCanvas));

        if (deleteYesButton != null)
        {
            deleteYesButton.onClick.RemoveAllListeners();
            deleteYesButton.onClick.AddListener(ConfirmDeleteJournal);
        }
    }

    private void ShowOnly(GameObject target)
    {
        homeCanvas.SetActive(target == homeCanvas);
        journalCanvas.SetActive(target == journalCanvas);
        deleteCanvas.SetActive(target == deleteCanvas);
    }

    private void ClearHomeList()
    {
        foreach (Transform c in homeContentParent)
            Destroy(c.gameObject);
    }

    private static void SetTMP(GameObject parent, string path, string value)
    {
        var t = parent.transform.Find(path)?.GetComponent<TextMeshProUGUI>();
        if (t != null) t.text = value;
    }

    private static string FormatCreatedAt(Journal j)
    {
        var dt = j.CreatedAt != default ? j.CreatedAt : j.UpdatedAt;
        return dt.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture);
    }

    // ----- validation helpers -----
    private void WireValidation()
    {
        titleInput.onValueChanged.RemoveAllListeners();
        contentInput.onValueChanged.RemoveAllListeners();

        titleInput.onValueChanged.AddListener(_ => RefreshValidation());
        contentInput.onValueChanged.AddListener(_ => RefreshValidation());

        RefreshValidation();
    }

    private void RefreshValidation()
    {
        string msg;
        bool ok = ValidateInputs(out msg);

        if (validationLabel) validationLabel.text = ok ? "" : msg;
        if (saveButton) saveButton.interactable = ok;
    }

    // Aturan baru: content WAJIB, title BOLEH kosong
    private bool ValidateInputs(out string message)
    {
        var c = (contentInput?.text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(c))
        {
            message = "Isi jurnal tidak boleh kosong.";
            return false;
        }

        message = null;
        return true;
    }

    // Ambil n kata pertama dari content untuk title otomatis
    private static string DeriveTitleFromContent(string content, int maxWords = 3, int maxChars = 60)
    {
        if (string.IsNullOrWhiteSpace(content)) return "Untitled";

        var words = Regex.Matches(content.Trim(), @"\S+");
        int take = Math.Min(maxWords, words.Count);

        string title = "";
        for (int i = 0; i < take; i++)
        {
            if (i > 0) title += " ";
            title += words[i].Value;
        }

        // keamanan panjang
        if (title.Length > maxChars)
            title = title.Substring(0, maxChars);

        return title;
    }

    #endregion

    #region Data

    private void FetchUserJournals()
    {
        var userId = PlayerPrefs.GetString(PrefKeyNickname, "");
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[JournalManager] No nickname in PlayerPrefs!");
            return;
        }

        StartCoroutine(
            ServiceManager.Instance.JournalService.FetchUserJournals(
                userId,
                OnJournalsReceived,
                err => Debug.LogError($"Fetch journals failed: {err}")
            )
        );
    }

    private void OnJournalsReceived(Journal[] journals)
    {
        ClearHomeList();

        // Tampilkan BG-NoNotes kalau kosong, sembunyikan kalau ada isi
        if (bgNoNotes != null)
            bgNoNotes.SetActive(journals == null || journals.Length == 0);

        if (journals == null) return;

        foreach (var j in journals)
        {
            var go = Instantiate(journalEntryPrefab, homeContentParent);
            go.name = $"Journal_{j.Id}";

            // Isi teks di prefab (ubah path jika hierarchy berbeda)
            SetTMP(go, "TitleText",   j.Title);
            SetTMP(go, "ContentText", j.Content);
            SetTMP(go, "TimestampBG/TimestampText", FormatCreatedAt(j));

            // Hook tombol Edit & Delete dari prefab
            var entry = go.GetComponent<JournalEntry>();
            if (entry == null) entry = go.AddComponent<JournalEntry>();
            entry.Setup(
                onEdit:   () => OpenEdit(j),
                onDelete: () => OpenDeleteDialog(j)
            );
        }
    }

    #endregion

    #region Create & Edit (JournalCanvas)

    private void OpenCreate()
    {
        _mode = JournalMode.Create;
        _currentJournal = null;

        if (headerText) headerText.text = "Create Journal";
        titleInput.text   = "";   // boleh kosong → nanti otomatis
        contentInput.text = "";

        ShowOnly(journalCanvas);
        WireValidation();
    }

    private void OpenEdit(Journal j)
    {
        _mode = JournalMode.Edit;
        _currentJournal = j;

        if (headerText) headerText.text = "Edit Journal";
        titleInput.text   = j.Title;
        contentInput.text = j.Content;

        ShowOnly(journalCanvas);
        WireValidation();
    }

    private void OnSaveClicked()
    {
        if (!ValidateInputs(out var _))
        {
            RefreshValidation();
            Debug.LogWarning("Content kosong — batal menyimpan.");
            return;
        }

        var rawTitle = (titleInput?.text ?? "").Trim();
        var content  = (contentInput?.text ?? "").Trim();

        // Jika title kosong → isi 3 kata pertama dari content
        var title = string.IsNullOrWhiteSpace(rawTitle)
                    ? DeriveTitleFromContent(content, 3, 60)
                    : rawTitle;

        var nowIso = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-ddTHH:mm:ss");

        if (_mode == JournalMode.Create)
        {
            StartCoroutine(
                ServiceManager.Instance.JournalService.CreateJournal(
                    PlayerPrefs.GetString(PrefKeyNickname, ""),
                    title,
                    content,
                    nowIso,
                    onSuccess: _ =>
                    {
                        ShowOnly(homeCanvas);
                        FetchUserJournals();
                    },
                    onError: err => Debug.LogError($"Create journal failed: {err}")
                )
            );
        }
        else // Edit
        {
            StartCoroutine(
                ServiceManager.Instance.JournalService.UpdateJournal(
                    _currentJournal.Id,
                    title,
                    content,
                    nowIso,
                    onSuccess: () =>
                    {
                        // Perbarui cache lokal ringan
                        _currentJournal.Title     = title;
                        _currentJournal.Content   = content;
                        _currentJournal.UpdatedAt = DateTime.UtcNow.AddHours(7);

                        ShowOnly(homeCanvas);
                        FetchUserJournals();
                    },
                    onError: err => Debug.LogError($"Update journal failed: {err}")
                )
            );
        }
    }

    #endregion

    #region Delete (DeleteCanvas)

    private void OpenDeleteDialog(Journal j)
    {
        _journalPendingDelete = j;
        if (deleteTitleText != null)
            deleteTitleText.text = $"Hapus \"{j.Title}\" ?";
        ShowOnly(deleteCanvas);
    }

    private void ConfirmDeleteJournal()
    {
        if (_journalPendingDelete == null)
        {
            ShowOnly(homeCanvas);
            return;
        }

        StartCoroutine(
            ServiceManager.Instance.JournalService.DeleteJournal(
                _journalPendingDelete.Id,
                onSuccess: () =>
                {
                    _journalPendingDelete = null;
                    ShowOnly(homeCanvas);
                    FetchUserJournals();
                },
                onError: err => Debug.LogError($"Delete journal failed: {err}")
            )
        );
    }

    #endregion
}