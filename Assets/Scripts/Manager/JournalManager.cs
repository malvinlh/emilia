// JournalManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;
using EMILIA.Data;  // your namespace for the Journal model

public class JournalManager : MonoBehaviour
{
    [Header("Entry List Prefab & Parent")]
    public GameObject   journalEntryPrefab;
    public Transform    contentParent;

    [Header("Canvases")]
    public GameObject homeCanvas;
    public GameObject journalPrevCanvas;
    public GameObject journalCreateCanvas1;
    public GameObject journalCreateCanvas2;
    public GameObject journalEditCanvas1;
    public GameObject journalEditCanvas2;

    [Header("Preview UI")]
    public TextMeshProUGUI journalPrevTitle;
    public TextMeshProUGUI journalPrevContent;
    public Button          prevBackButton;
    public Button          prevEditButton;
    public Button          deleteButton;

    [Header("Create Step 1 UI")]
    public Button       newJournalButton;
    public TMP_InputField createTitleInput;
    public TextMeshProUGUI createDateText;
    public Button          createNextButton;
    public Button          createCancelStep1Button;

    [Header("Create Step 2 UI")]
    public TMP_InputField  createContentInput;
    public Button          createSaveButton;
    public Button          createCancelStep2Button;

    [Header("Edit Step 1 UI")]
    public TMP_InputField  editTitleInput;
    public TextMeshProUGUI editDateText;
    public Button          editNextButton;
    public Button          editCancelStep1Button;

    [Header("Edit Step 2 UI")]
    public TMP_InputField  editContentInput;
    public Button          editSaveButton;
    public Button          editCancelStep2Button;

    [Header("Delete Step")]
    public GameObject deleteJournalCanvas;
    public Button     deleteYesButton;
    public Button     deleteNoButton;

    // state
    private Journal currentJournal;
    private Journal journalPendingDelete;

    void Start()
    {
        // initial canvas setup
        homeCanvas.SetActive(true);
        journalPrevCanvas.SetActive(false);
        journalCreateCanvas1.SetActive(false);
        journalCreateCanvas2.SetActive(false);
        journalEditCanvas1.SetActive(false);
        journalEditCanvas2.SetActive(false);
        deleteJournalCanvas.SetActive(false);

        // preview back
        prevBackButton.onClick.AddListener(() =>
        {
            journalPrevCanvas.SetActive(false);
            homeCanvas.SetActive(true);
        });

        // create flow
        newJournalButton.onClick.AddListener(() =>
        {
            homeCanvas.SetActive(false);
            journalCreateCanvas1.SetActive(true);
            var wibNow = DateTime.UtcNow.AddHours(7);
            createDateText.text = wibNow.ToString("dd MMMM yyyy hh:mm tt");
            createTitleInput.text = "";
        });
        createCancelStep1Button.onClick.AddListener(() =>
        {
            journalCreateCanvas1.SetActive(false);
            homeCanvas.SetActive(true);
        });
        createNextButton.onClick.AddListener(OpenCreateStep2);
        createCancelStep2Button.onClick.AddListener(() =>
        {
            journalCreateCanvas2.SetActive(false);
            journalCreateCanvas1.SetActive(true);
        });
        createSaveButton.onClick.AddListener(SaveNewJournal);

        // edit flow
        editCancelStep1Button.onClick.AddListener(() =>
        {
            journalEditCanvas1.SetActive(false);
            homeCanvas.SetActive(true);
        });
        editCancelStep2Button.onClick.AddListener(() =>
        {
            journalEditCanvas2.SetActive(false);
            journalEditCanvas1.SetActive(true);
        });

        // delete dialog
        deleteNoButton.onClick.AddListener(() =>
        {
            deleteJournalCanvas.SetActive(false);
            homeCanvas.SetActive(true);
        });
        deleteYesButton.onClick.AddListener(ConfirmDeleteJournal);

        // fetch initial list
        var userId = PlayerPrefs.GetString("Nickname", "");
        if (string.IsNullOrEmpty(userId))
        {
            Debug.LogError("[JournalManager] No nickname in PlayerPrefs!");
            return;
        }

        StartCoroutine(
            ServiceManager.Instance.JournalService.FetchUserJournals(
                userId,
                OnJournalsReceived,
                err => Debug.LogError("Fetch journals failed: " + err)
            )
        );
    }

    private void OnJournalsReceived(Journal[] journals)
    {
        // clear
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        // populate
        foreach (var j in journals)
        {
            var go = Instantiate(journalEntryPrefab, contentParent);
            go.name = $"Journal_{j.Id}";

            // title
            var titleText = go.transform.Find("TitleText")?
                                .GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = j.Title;
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.overflowMode     = TextOverflowModes.Ellipsis;
            }

            // content
            var contentText = go.transform.Find("ContentText")?
                                  .GetComponent<TextMeshProUGUI>();
            if (contentText != null)
            {
                contentText.text = j.Content;
                contentText.textWrappingMode = TextWrappingModes.Normal;
                contentText.overflowMode     = TextOverflowModes.Ellipsis;
            }

            // date (use UpdatedAt if later, else CreatedAt)
            var dateText = go.transform.Find("DateBG/DateText")?
                                 .GetComponent<TextMeshProUGUI>();
            if (dateText != null)
            {
                var dt = j.UpdatedAt > j.CreatedAt ? j.UpdatedAt : j.CreatedAt;
                dateText.text = dt.ToString("dd MMMM yyyy");
            }

            // click handler
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            var entry = go.AddComponent<JournalEntry>();
            entry.Setup(j, ShowPreview);
        }
    }

    private void ShowPreview(Journal j)
    {
        currentJournal = j;
        homeCanvas.SetActive(false);
        journalPrevCanvas.SetActive(true);

        journalPrevTitle.text   = j.Title;
        journalPrevContent.text = j.Content;

        prevEditButton.onClick.RemoveAllListeners();
        prevEditButton.onClick.AddListener(OpenEditStep1);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => ShowDeleteDialog(j));
    }

    private void OpenEditStep1()
    {
        journalPrevCanvas.SetActive(false);
        journalEditCanvas1.SetActive(true);

        editTitleInput.text = currentJournal.Title;

        var created = currentJournal.CreatedAt;
        var updated = currentJournal.UpdatedAt > created
                        ? currentJournal.UpdatedAt
                        : created;
        editDateText.text = $"{created:dd MMMM yyyy hh:mm tt} / {updated:dd MMMM yyyy hh:mm tt}";

        editNextButton.onClick.RemoveAllListeners();
        editNextButton.onClick.AddListener(OpenEditStep2);
    }

    private void OpenEditStep2()
    {
        journalEditCanvas1.SetActive(false);
        journalEditCanvas2.SetActive(true);

        editContentInput.text = currentJournal.Content;

        editSaveButton.onClick.RemoveAllListeners();
        editSaveButton.onClick.AddListener(SaveEdits);
    }

    private void SaveEdits()
    {
        var newTitle   = editTitleInput.text.Trim();
        var newContent = editContentInput.text.Trim();

        if (newTitle == currentJournal.Title && newContent == currentJournal.Content)
        {
            journalEditCanvas2.SetActive(false);
            homeCanvas.SetActive(true);
            return;
        }

        var wibNow = DateTime.UtcNow.AddHours(7);
        var iso    = wibNow.ToString("yyyy-MM-ddTHH:mm:ss");

        StartCoroutine(
            ServiceManager.Instance.JournalService.UpdateJournal(
                currentJournal.Id,
                newTitle,
                newContent,
                iso,
                onSuccess: () =>
                {
                    // update local model
                    currentJournal.Title     = newTitle;
                    currentJournal.Content   = newContent;
                    currentJournal.UpdatedAt = wibNow;

                    journalEditCanvas2.SetActive(false);
                    homeCanvas.SetActive(true);
                    RefreshJournals();
                },
                onError: err => Debug.LogError("Update journal failed: " + err)
            )
        );
    }

    private void OpenCreateStep2()
    {
        journalCreateCanvas1.SetActive(false);
        journalCreateCanvas2.SetActive(true);
        createContentInput.text = "";
    }

    private void SaveNewJournal()
    {
        var title   = createTitleInput.text.Trim();
        var content = createContentInput.text.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            Debug.LogWarning("Title or content empty—skipping create.");
            return;
        }

        var wibNow = DateTime.UtcNow.AddHours(7);
        var iso    = wibNow.ToString("yyyy-MM-ddTHH:mm:ss");

        StartCoroutine(
            ServiceManager.Instance.JournalService.CreateJournal(
                PlayerPrefs.GetString("Nickname", ""),
                title,
                content,
                iso,
                onSuccess: _ =>
                {
                    journalCreateCanvas2.SetActive(false);
                    homeCanvas.SetActive(true);
                    RefreshJournals();
                },
                onError: err => Debug.LogError("Create journal failed: " + err)
            )
        );
    }

    private void ShowDeleteDialog(Journal j)
    {
        journalPendingDelete = j;
        journalPrevCanvas.SetActive(false);
        deleteJournalCanvas.SetActive(true);
    }

    public void ConfirmDeleteJournal()
    {
        StartCoroutine(
            ServiceManager.Instance.JournalService.DeleteJournal(
                journalPendingDelete.Id,
                onSuccess: () =>
                {
                    deleteJournalCanvas.SetActive(false);
                    homeCanvas.SetActive(true);
                    RefreshJournals();
                },
                onError: err => Debug.LogError("Delete journal failed: " + err)
            )
        );
    }

    private void RefreshJournals()
    {
        StartCoroutine(
            ServiceManager.Instance.JournalService.FetchUserJournals(
                PlayerPrefs.GetString("Nickname", ""),
                OnJournalsReceived,
                err => Debug.LogError("Fetch journals failed: " + err)
            )
        );
    }
}

// helper for entry clicks
public class JournalEntry : MonoBehaviour, IPointerClickHandler
{
    private Journal         data;
    private Action<Journal> callback;

    public void Setup(Journal j, Action<Journal> onClick)
    {
        data     = j;
        callback = onClick;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        callback?.Invoke(data);
    }
}