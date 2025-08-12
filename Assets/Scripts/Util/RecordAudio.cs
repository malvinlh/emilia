using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class RecordAudio : MonoBehaviour
{
    #region Inspector Fields

    [Header("Recording Settings")]
    [Tooltip("Name of the saved WAV file (will be placed under persistentDataPath/Recordings)")]
    [SerializeField] private string outputFileName = "recording.wav";

    [Tooltip("Subfolder under persistentDataPath where recordings go (ignored if absolute path is used below)")]
    [SerializeField] private string folderName = "Recordings";

    [Tooltip("Which mic device to use (blank = first available)")]
    [SerializeField] private string micDevice = "";

    [Tooltip("Sample rate in Hz")]
    [SerializeField] private int sampleRate = 44100;

    [Tooltip("Max recording length in seconds")]
    [SerializeField] private int maxLengthSec = 3599;

    [Header("UI Buttons")]
    [Tooltip("Button that starts the recording")]
    [SerializeField] private Button startButton;      // MicStartButton

    [Tooltip("Button that stops & saves the recording")]
    [SerializeField] private Button stopButton;       // MicStopButton

    [Header("UI Ownership")]
    [Tooltip("If true, this component toggles Start/Stop buttons itself. If false, external (ChatManager) should call SetUIState.")]
    [SerializeField] private bool manageButtonsInternally = false;

    #endregion

    #region Events

    /// <summary>
    /// Dipanggil setelah file WAV selesai disimpan. Param = absolute path file.
    /// </summary>
    public event Action<string> OnSaved;

    /// <summary>
    /// Dipanggil saat status mic berubah. true = mulai merekam, false = berhenti.
    /// </summary>
    public event Action<bool> OnMicStateChanged;

    #endregion

    #region Public State

    /// <summary>
    /// Status perekaman saat ini.
    /// </summary>
    public bool IsRecording { get; private set; }

    #endregion

    #region Private State

    private string    _directoryPath;
    private string    _filePath;
    private AudioClip _recordedClip;
    private float     _startTime;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        // ---- Path tujuan ----
        // Jika ingin folder absolut khusus Windows (seperti contoh lama), isi di sini:
        string targetFolder = @"D:\Emilia\AI\Recordings";
        // Jika tidak, gunakan persistentDataPath/folderName (cross-platform):
        //string targetFolder = Path.Combine(Application.persistentDataPath, folderName);

        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        _directoryPath = targetFolder;
        _filePath      = Path.Combine(_directoryPath, outputFileName);

        // Initial UI state (hanya bila dikelola internal)
        if (manageButtonsInternally)
        {
            if (startButton) { startButton.gameObject.SetActive(true);  startButton.interactable = true; }
            if (stopButton)  { stopButton .gameObject.SetActive(false); stopButton .interactable = true; }
        }
        else
        {
            // External owner (ChatManager) yang akan memanggil SetUIState()
            if (startButton) startButton.gameObject.SetActive(true);
            if (stopButton)  stopButton .gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (startButton) startButton.onClick.AddListener(OnStartClicked);
        if (stopButton)  stopButton .onClick.AddListener(OnStopClicked);
    }

    private void OnDisable()
    {
        if (startButton) startButton.onClick.RemoveListener(OnStartClicked);
        if (stopButton)  stopButton .onClick.RemoveListener(OnStopClicked);
    }

    #endregion

    #region UI API (External Control)

    /// <summary>
    /// Dipanggil dari ChatManager untuk menyelaraskan UI start/stop & interaksi tombol.
    /// </summary>
    public void SetUIState(bool micOn, bool interactable)
    {
        if (startButton)
        {
            startButton.gameObject.SetActive(!micOn);
            startButton.interactable = interactable && !micOn;
        }
        if (stopButton)
        {
            stopButton.gameObject.SetActive(micOn);
            stopButton.interactable = interactable && micOn;
        }
    }

    #endregion

    #region UI Callbacks

    private void OnStartClicked()
    {
        StartRecording();

        if (manageButtonsInternally)
        {
            if (startButton) { startButton.gameObject.SetActive(false); startButton.interactable = false; }
            if (stopButton)  { stopButton .gameObject.SetActive(true);  stopButton .interactable = true; }
        }

        OnMicStateChanged?.Invoke(true);
    }

    private void OnStopClicked()
    {
        StopRecordingAndSave();

        if (manageButtonsInternally)
        {
            if (stopButton)  { stopButton .gameObject.SetActive(false); stopButton .interactable = false; }
            if (startButton) { startButton.gameObject.SetActive(true);  startButton.interactable = true; }
        }

        OnMicStateChanged?.Invoke(false);
    }

    #endregion

    #region Recording Logic

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
            return;
        }

        string device = string.IsNullOrEmpty(micDevice) ? Microphone.devices[0] : micDevice;

        _recordedClip = Microphone.Start(device, loop: false, lengthSec: maxLengthSec, frequency: sampleRate);
        _startTime    = Time.realtimeSinceStartup;
        IsRecording   = true;

        Debug.Log($"Recording started on '{device}'");
    }

    /// <summary>
    /// Stop mic dan simpan file WAV (memicu event OnSaved).
    /// </summary>
    public void StopRecordingAndSave()
    {
        StopRecording();

        if (_recordedClip == null)
            return;

        float recordedSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - _startTime);
        var trimmed = TrimClip(_recordedClip, recordedSeconds);

        try
        {
            WavUtility.Save(_filePath, trimmed);
            Debug.Log($"Recording saved to: {_filePath}");
            OnSaved?.Invoke(_filePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save WAV: {e.Message}");
        }

        // bersihkan
        _recordedClip = null;
    }

    /// <summary>
    /// Hanya mematikan microphone & menandai status; tidak menyimpan file.
    /// </summary>
    public void StopRecording()
    {
        if (!IsRecording) return;

        try { Microphone.End(null); }
        catch (Exception e) { Debug.LogWarning($"Microphone.End error: {e.Message}"); }

        IsRecording = false;
    }

    private AudioClip TrimClip(AudioClip clip, float lengthSeconds)
    {
        int samples = Mathf.Clamp((int)(lengthSeconds * clip.frequency), 0, clip.samples);
        var data = new float[samples * clip.channels];
        clip.GetData(data, 0);

        var newClip = AudioClip.Create(
            name: clip.name,
            lengthSamples: samples,
            channels: clip.channels,
            frequency: clip.frequency,
            stream: false
        );
        newClip.SetData(data, 0);
        return newClip;
    }

    #endregion
}