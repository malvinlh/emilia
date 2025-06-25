// JournalManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

public class JournalManager : MonoBehaviour
{
    [Header("Entry List Prefab & Parent")]
    public GameObject   journalEntryPrefab;  // Prefab Image + TitleText, ContentText, DateBG/DateText
    public Transform    contentParent;       // Scroll View → Viewport → Content

    [Header("Canvases")]
    public GameObject homeCanvas;            // Panel list jurnal
    public GameObject journalPrevCanvas;     // Panel preview
    public GameObject   journalCreateCanvas1;  // Panel step1 create
    public GameObject   journalCreateCanvas2;  // Panel step2 create
    public GameObject journalEditCanvas1;    // Step 1: edit title & dates
    public GameObject journalEditCanvas2;    // Step 2: edit content

    [Header("Preview UI")]
    public TextMeshProUGUI journalPrevTitle;
    public TextMeshProUGUI journalPrevContent;
    public Button          prevBackButton;
    public Button          prevEditButton;

    [Header("Create Step 1 UI")]
    public Button       newJournalButton;      // tombol “+” di HomeCanvas

    public TMP_InputField  createTitleInput;
    public TextMeshProUGUI createDateText;
    public Button          createNextButton;
    public Button          createCancelStep1Button;

    [Header("Create Step 2 UI")]
    public TMP_InputField  createContentInput;
    public Button          createSaveButton;
    public Button          createCancelStep2Button;
    
    [Header("Edit Step 1 UI")]
    public TMP_InputField  editTitleInput;
    public TextMeshProUGUI editDateText;     // single field: "Created / Updated"
    public Button          editNextButton;
    public Button          editCancelStep1Button;

    [Header("Edit Step 2 UI")]
    public TMP_InputField  editContentInput;
    public Button          editSaveButton;
    public Button          editCancelStep2Button;

    // state of the currently previewed journal
    private Journal currentJournal;

    void Start()
    {
        // 1) Show home, hide others
        homeCanvas.SetActive(true);
        journalPrevCanvas.SetActive(false);
        journalEditCanvas1.SetActive(false);
        journalEditCanvas2.SetActive(false);

        // 2) Hook Back on preview → home
        prevBackButton.onClick.AddListener(() =>
        {
            journalPrevCanvas.SetActive(false);
            homeCanvas.SetActive(true);
        });

        // 3) Hook Cancel on edit step1 → home
        editCancelStep1Button.onClick.AddListener(() =>
        {
            journalEditCanvas1.SetActive(false);
            homeCanvas.SetActive(true);
        });

        // 4) Hook Cancel on edit step2 → step1
        editCancelStep2Button.onClick.AddListener(() =>
        {
            journalEditCanvas2.SetActive(false);
            journalEditCanvas1.SetActive(true);
        });

        newJournalButton.onClick.AddListener(() =>
        {
            homeCanvas.SetActive(false);
            journalCreateCanvas1.SetActive(true);

            // isi timestamp saat panel muncul (WIB = UTC+7)
            DateTime wibNow = DateTime.UtcNow.AddHours(7);
            createDateText.text = wibNow.ToString("dd MMMM yyyy hh:mm tt");

            // reset title field
            createTitleInput.text = "";
        });

        createCancelStep1Button.onClick.AddListener(() =>
        {
            journalCreateCanvas1.SetActive(false);
            homeCanvas.SetActive(true);
        });

        createCancelStep2Button.onClick.AddListener(() =>
        {
            journalCreateCanvas2.SetActive(false);
            journalCreateCanvas1.SetActive(true);
        });

        createNextButton.onClick.AddListener(OpenCreateStep2);
        createSaveButton.onClick.AddListener(SaveNewJournal);

        // 5) Fetch journals list
        string userId = PlayerPrefs.GetString("Nickname", "");
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
        // clear old entries
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        for (int i = 0; i < journals.Length; i++)
        {
            var j   = journals[i];
            var go  = Instantiate(journalEntryPrefab, contentParent);
            go.name = $"Journal{i+1}";

            // 1) Title snippet
            var titleText = go.transform.Find("TitleText")?
                                .GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.textWrappingMode = TextWrappingModes.NoWrap;
                titleText.overflowMode       = TextOverflowModes.Ellipsis;
                titleText.text               = j.title;
            }

            // 2) Content snippet
            var contentText = go.transform.Find("ContentText")?
                                  .GetComponent<TextMeshProUGUI>();
            if (contentText != null)
            {
                contentText.textWrappingMode = TextWrappingModes.Normal;
                contentText.overflowMode       = TextOverflowModes.Ellipsis;
                contentText.text               = j.content;
            }

            // 3) Date snippet: gunakan updated_at jika ada, otherwise created_at
            var dateText = go.transform
                            .Find("DateBG/DateText")
                            ?.GetComponent<TextMeshProUGUI>();
            if (dateText != null)
            {
                DateTime dt;
                if (!string.IsNullOrEmpty(j.updated_at)
                    && DateTime.TryParse(j.updated_at, out dt))
                {
                    dateText.text = dt.ToString("dd MMMM yyyy");
                }
                else if (DateTime.TryParse(j.created_at, out dt))
                {
                    dateText.text = dt.ToString("dd MMMM yyyy");
                }
            }

            // 4) Click handler (Image must have RaycastTarget=true)
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;

            var entry = go.AddComponent<JournalEntry>();
            entry.Setup(j, ShowPreview);
        }
    }

    private void ShowPreview(Journal j)
    {
        currentJournal = j;

        // switch canvases
        homeCanvas.SetActive(false);
        journalPrevCanvas.SetActive(true);

        // fill preview
        journalPrevTitle.text   = j.title;
        journalPrevContent.text = j.content;

        // hook Edit button
        prevEditButton.onClick.RemoveAllListeners();
        prevEditButton.onClick.AddListener(OpenEditStep1);
    }

    private void OpenEditStep1()
    {
        // preview → edit step1
        journalPrevCanvas.SetActive(false);
        journalEditCanvas1.SetActive(true);

        // fill title
        editTitleInput.text = currentJournal.title;

        // build combined date string
        DateTime created = DateTime.Parse(currentJournal.created_at);
        DateTime updated = string.IsNullOrEmpty(currentJournal.updated_at)
                            ? created
                            : DateTime.Parse(currentJournal.updated_at);

        // display "Created / Updated"
        editDateText.text = $"{created:dd MMMM yyyy hh:mm tt} / {updated:dd MMMM yyyy hh:mm tt}";

        // hook Next
        editNextButton.onClick.RemoveAllListeners();
        editNextButton.onClick.AddListener(OpenEditStep2);
    }

    private void OpenEditStep2()
    {
        // step1 → step2
        journalEditCanvas1.SetActive(false);
        journalEditCanvas2.SetActive(true);

        // fill content input
        editContentInput.text = currentJournal.content;

        // hook Save
        editSaveButton.onClick.RemoveAllListeners();
        editSaveButton.onClick.AddListener(SaveEdits);
    }

    private void SaveEdits()
    {
        string newTitle   = editTitleInput.text.Trim();
        string newContent = editContentInput.text.Trim();

        bool didChange = newTitle   != currentJournal.title
                    || newContent != currentJournal.content;

        if (!didChange)
        {
            journalEditCanvas2.SetActive(false);
            homeCanvas.SetActive(true);
            return;
        }

        DateTime wibNow = DateTime.UtcNow.AddHours(7);
        string isoUpdatedAt = wibNow.ToString("yyyy-MM-ddTHH:mm:ss");

        // kirim PATCH
        StartCoroutine(
            ServiceManager.Instance.JournalService.UpdateJournal(
                currentJournal.id,
                newTitle,
                newContent,
                isoUpdatedAt,
                onSuccess: () =>
                {
                    // update model lokal
                    currentJournal.title      = newTitle;
                    currentJournal.content    = newContent;
                    currentJournal.updated_at = isoUpdatedAt;

                    // tutup edit canvases
                    journalEditCanvas2.SetActive(false);
                    homeCanvas.SetActive(true);

                    // **refresh UI list** dengan mem‐Fetch ulang
                    StartCoroutine(
                        ServiceManager.Instance.JournalService.FetchUserJournals(
                            PlayerPrefs.GetString("Nickname", ""),
                            OnJournalsReceived,
                            err => Debug.LogError("Fetch journals failed: " + err)
                        )
                    );
                },
                onError: err => Debug.LogError("Update journal failed: " + err)
            )
        );
    }

    private void OpenCreateStep2()
    {
        // pindah dari step1 → step2
        journalCreateCanvas1.SetActive(false);
        journalCreateCanvas2.SetActive(true);

        // isi content input kosong
        createContentInput.text = "";
    }

    private void SaveNewJournal()
    {
        string title   = createTitleInput.text.Trim();
        string content = createContentInput.text.Trim();
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            Debug.LogWarning("Title or content empty—skipping create.");
            return;
        }

        DateTime wibNow = DateTime.UtcNow.AddHours(7);
        string iso = wibNow.ToString("yyyy-MM-ddTHH:mm:ss");

        StartCoroutine(
            ServiceManager.Instance.JournalService.CreateJournal(
                PlayerPrefs.GetString("Nickname", ""),
                title,
                content,
                iso,
                onSuccess: newJournal =>
                {
                    // kembali ke home
                    journalCreateCanvas2.SetActive(false);
                    homeCanvas.SetActive(true);

                    // optionally tambahkan langsung ke list tanpa reload penuh:
                    // Instantiate satu entry baru diakhir contentParent
                    // Anda bisa panggil OnJournalsReceived sekali lagi jika ingin reload semua.
                    StartCoroutine(
                        ServiceManager.Instance.JournalService.FetchUserJournals(
                            PlayerPrefs.GetString("Nickname", ""),
                            OnJournalsReceived,
                            err => Debug.LogError("Re-fetch after create failed: " + err)
                        )
                    );
                },
                onError: err => Debug.LogError("Create journal failed: " + err)
            )
        );
    }
}

// helper untuk click tanpa Button
public class JournalEntry : MonoBehaviour, IPointerClickHandler
{
    private Journal                data;
    private Action<Journal>        callback;

    public void Setup(Journal j, Action<Journal> onClick)
    {
        data     = j;
        callback = onClick;
    }

    public void OnPointerClick(PointerEventData evt)
    {
        callback?.Invoke(data);
    }
}
