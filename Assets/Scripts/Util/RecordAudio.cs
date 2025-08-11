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
    [Tooltip("Subfolder under persistentDataPath where recordings go")]
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

    #endregion

    #region Events

    /// <summary>
    /// Dipanggil setelah file WAV selesai disimpan. Param = absolute path file.
    /// </summary>
    public event Action<string> OnSaved;

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
        // Path tujuan di drive D:
        string targetFolder = @"D:\Emilia\AI\Recordings";

        // Pastikan folder ada
        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        // Simpan path ke variabel class
        _directoryPath = targetFolder;
        _filePath = Path.Combine(_directoryPath, outputFileName);

        // Initial UI state
        if (startButton) startButton.gameObject.SetActive(true);
        if (stopButton)  stopButton .gameObject.SetActive(false);
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

    #region UI Callbacks

    private void OnStartClicked()
    {
        StartRecording();
        if (startButton) startButton.gameObject.SetActive(false);
        if (stopButton)  stopButton .gameObject.SetActive(true);
    }

    private void OnStopClicked()
    {
        StopRecording();
        if (stopButton)  stopButton .gameObject.SetActive(false);
        if (startButton) startButton.gameObject.SetActive(true);
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

        Debug.Log($"Recording started on '{device}'");
    }

    public void StopRecording()
    {
        if (_recordedClip == null)
            return;

        Microphone.End(null);

        float recordedSeconds = Time.realtimeSinceStartup - _startTime;
        var trimmed = TrimClip(_recordedClip, recordedSeconds);

        WavUtility.Save(_filePath, trimmed);
        Debug.Log($"Recording saved to: {_filePath}");

        _recordedClip = null;

        // Trigger event agar ChatManager bisa upload
        OnSaved?.Invoke(_filePath);
    }

    private AudioClip TrimClip(AudioClip clip, float length)
    {
        int samples = Mathf.Min((int)(length * clip.frequency), clip.samples);
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