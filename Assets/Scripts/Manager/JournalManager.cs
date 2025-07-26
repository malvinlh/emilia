using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using EMILIA.Data;

public class JournalManager : MonoBehaviour
{
    #region Inspector Fields

    [Header("Entry List Prefab & Parent")]
    [SerializeField] private GameObject _journalEntryPrefab;
    [SerializeField] private Transform _contentParent;

    [Header("Canvases")]
    [SerializeField] private GameObject _homeCanvas;
    [SerializeField] private GameObject _previewCanvas;
    [SerializeField] private GameObject _createCanvasStep1;
    [SerializeField] private GameObject _createCanvasStep2;
    [SerializeField] private GameObject _editCanvasStep1;
    [SerializeField] private GameObject _editCanvasStep2;
    [SerializeField] private GameObject _deleteDialogCanvas;

    [Header("Preview UI")]
    [SerializeField] private TextMeshProUGUI _previewTitle;
    [SerializeField] private TextMeshProUGUI _previewContent;
    [SerializeField] private Button _previewBackButton;
    [SerializeField] private Button _previewEditButton;
    [SerializeField] private Button _previewDeleteButton;

    [Header("Create Step 1 UI")]
    [SerializeField] private Button _newJournalButton;
    [SerializeField] private TMP_InputField _createTitleInput;
    [SerializeField] private TextMeshProUGUI _createDateText;
    [SerializeField] private Button _createNextButton;
    [SerializeField] private Button _createCancel1Button;

    [Header("Create Step 2 UI")]
    [SerializeField] private TMP_InputField _createContentInput;
    [SerializeField] private Button _createSaveButton;
    [SerializeField] private Button _createCancel2Button;

    [Header("Edit Step 1 UI")]
    [SerializeField] private TMP_InputField _editTitleInput;
    [SerializeField] private TextMeshProUGUI _editDateText;
    [SerializeField] private Button _editNextButton;
    [SerializeField] private Button _editCancel1Button;

    [Header("Edit Step 2 UI")]
    [SerializeField] private TMP_InputField _editContentInput;
    [SerializeField] private Button _editSaveButton;
    [SerializeField] private Button _editCancel2Button;

    [Header("Delete Dialog")]
    [SerializeField] private Button _deleteYesButton;
    [SerializeField] private Button _deleteNoButton;

    #endregion

    #region Fields & Constants

    private const string PrefKeyNickname = "Nickname";
    private Journal _currentJournal;
    private Journal _journalPendingDelete;

    #endregion

    #region Unity Callbacks

    private void Start()
    {
        InitializeCanvases();
        BindUIEvents();
        FetchUserJournals();
    }

    #endregion

    #region Initialization

    private void InitializeCanvases()
    {
        _homeCanvas.SetActive(true);
        _previewCanvas.SetActive(false);
        _createCanvasStep1.SetActive(false);
        _createCanvasStep2.SetActive(false);
        _editCanvasStep1.SetActive(false);
        _editCanvasStep2.SetActive(false);
        _deleteDialogCanvas.SetActive(false);
    }

    private void BindUIEvents()
    {
        _previewBackButton.onClick.AddListener(() => SwitchTo(_homeCanvas, _previewCanvas));
        _newJournalButton.onClick.AddListener(OpenCreateStep1);
        _createCancel1Button.onClick.AddListener(() => SwitchTo(_homeCanvas, _createCanvasStep1));
        _createNextButton.onClick.AddListener(OpenCreateStep2);
        _createCancel2Button.onClick.AddListener(() => SwitchTo(_createCanvasStep1, _createCanvasStep2));
        _createSaveButton.onClick.AddListener(SaveNewJournal);

        _editCancel1Button.onClick.AddListener(() => SwitchTo(_homeCanvas, _editCanvasStep1));
        _editCancel2Button.onClick.AddListener(() => SwitchTo(_editCanvasStep1, _editCanvasStep2));

        _deleteNoButton.onClick.AddListener(() => SwitchTo(_homeCanvas, _deleteDialogCanvas));
        _deleteYesButton.onClick.AddListener(ConfirmDeleteJournal);
    }

    private void SwitchTo(GameObject on, GameObject off = null)
    {
        if (off != null) off.SetActive(false);
        on.SetActive(true);
    }

    #endregion

    #region Data Fetching

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
        ClearEntries();
        foreach (var j in journals)
            CreateJournalEntry(j);
    }

    private void ClearEntries()
    {
        foreach (Transform child in _contentParent)
            Destroy(child.gameObject);
    }

    #endregion

    #region Journal Entries

    private void CreateJournalEntry(Journal j)
    {
        var go = Instantiate(_journalEntryPrefab, _contentParent);
        go.name = $"Journal_{j.Id}";

        // Title
        SetText(go, "TitleText", j.Title, TextWrappingModes.NoWrap, TextOverflowModes.Ellipsis);
        // Content snippet
        SetText(go, "ContentText", j.Content, TextWrappingModes.Normal, TextOverflowModes.Ellipsis);
        // Date
        SetText(go, "DateBG/DateText", GetDisplayDate(j));

        // Enable click
        if (go.TryGetComponent<Image>(out var img))
            img.raycastTarget = true;

        var entry = go.AddComponent<JournalEntry>();
        entry.Setup(j, ShowPreview);
    }

    private void SetText(GameObject parent, string path, string text,
                         TextWrappingModes wrap = default, TextOverflowModes overflow = default)
    {
        var comp = parent.transform.Find(path)?.GetComponent<TextMeshProUGUI>();
        if (comp == null) return;
        comp.text = text;
        comp.textWrappingMode = wrap;
        comp.overflowMode     = overflow;
    }

    private string GetDisplayDate(Journal j)
    {
        var dt = (j.UpdatedAt > j.CreatedAt) ? j.UpdatedAt : j.CreatedAt;
        return dt.ToString("dd MMMM yyyy");
    }

    #endregion

    #region Preview Flow

    private void ShowPreview(Journal j)
    {
        _currentJournal = j;
        SwitchTo(_previewCanvas, _homeCanvas);

        _previewTitle.text   = j.Title;
        _previewContent.text = j.Content;

        _previewEditButton.onClick.RemoveAllListeners();
        _previewEditButton.onClick.AddListener(OpenEditStep1);

        _previewDeleteButton.onClick.RemoveAllListeners();
        _previewDeleteButton.onClick.AddListener(() => ShowDeleteDialog(j));
    }

    #endregion

    #region Create Flow

    private void OpenCreateStep1()
    {
        SwitchTo(_createCanvasStep1, _homeCanvas);
        _createTitleInput.text = "";
        _createDateText.text   = DateTime.UtcNow.AddHours(7).ToString("dd MMMM yyyy hh:mm tt");
    }

    private void OpenCreateStep2()
    {
        SwitchTo(_createCanvasStep2, _createCanvasStep1);
        _createContentInput.text = "";
    }

    private void SaveNewJournal()
    {
        var title   = _createTitleInput.text.Trim();
        var content = _createContentInput.text.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            Debug.LogWarning("Title or content empty—skipping create.");
            return;
        }

        var iso = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-ddTHH:mm:ss");
        StartCoroutine(
            ServiceManager.Instance.JournalService.CreateJournal(
                PlayerPrefs.GetString(PrefKeyNickname, ""),
                title,
                content,
                iso,
                onSuccess: _ =>
                {
                    SwitchTo(_homeCanvas, _createCanvasStep2);
                    FetchUserJournals();
                },
                onError: err => Debug.LogError($"Create journal failed: {err}")
            )
        );
    }

    #endregion

    #region Edit Flow

    private void OpenEditStep1()
    {
        SwitchTo(_editCanvasStep1, _previewCanvas);
        _editTitleInput.text = _currentJournal.Title;
        _editDateText.text  = $"{_currentJournal.CreatedAt:dd MMMM yyyy hh:mm tt} / {(_currentJournal.UpdatedAt > _currentJournal.CreatedAt ? _currentJournal.UpdatedAt : _currentJournal.CreatedAt):dd MMMM yyyy hh:mm tt}";
        _editNextButton.onClick.RemoveAllListeners();
        _editNextButton.onClick.AddListener(OpenEditStep2);
    }

    private void OpenEditStep2()
    {
        SwitchTo(_editCanvasStep2, _editCanvasStep1);
        _editContentInput.text = _currentJournal.Content;
        _editSaveButton.onClick.RemoveAllListeners();
        _editSaveButton.onClick.AddListener(SaveEdits);
    }

    private void SaveEdits()
    {
        var newTitle   = _editTitleInput.text.Trim();
        var newContent = _editContentInput.text.Trim();
        if (newTitle == _currentJournal.Title && newContent == _currentJournal.Content)
        {
            SwitchTo(_homeCanvas, _editCanvasStep2);
            return;
        }

        var nowIso = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-ddTHH:mm:ss");
        StartCoroutine(
            ServiceManager.Instance.JournalService.UpdateJournal(
                _currentJournal.Id,
                newTitle,
                newContent,
                nowIso,
                onSuccess: () =>
                {
                    _currentJournal.Title     = newTitle;
                    _currentJournal.Content   = newContent;
                    _currentJournal.UpdatedAt = DateTime.UtcNow.AddHours(7);

                    SwitchTo(_homeCanvas, _editCanvasStep2);
                    FetchUserJournals();
                },
                onError: err => Debug.LogError($"Update journal failed: {err}")
            )
        );
    }

    #endregion

    #region Delete Flow

    private void ShowDeleteDialog(Journal j)
    {
        _journalPendingDelete = j;
        SwitchTo(_deleteDialogCanvas, _previewCanvas);
    }

    private void ConfirmDeleteJournal()
    {
        StartCoroutine(
            ServiceManager.Instance.JournalService.DeleteJournal(
                _journalPendingDelete.Id,
                onSuccess: () =>
                {
                    SwitchTo(_homeCanvas, _deleteDialogCanvas);
                    FetchUserJournals();
                },
                onError: err => Debug.LogError($"Delete journal failed: {err}")
            )
        );
    }

    #endregion
}

// Click handler for journal entries
public class JournalEntry : MonoBehaviour, IPointerClickHandler
{
    private Journal _data;
    private Action<Journal> _onClick;

    public void Setup(Journal data, Action<Journal> onClick)
    {
        _data    = data;
        _onClick = onClick;
    }

    public void OnPointerClick(PointerEventData eventData)
        => _onClick?.Invoke(_data);
}