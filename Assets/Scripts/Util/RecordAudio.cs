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
    [SerializeField] private Button startButton;
    [Tooltip("Button that stops & saves the recording")]
    [SerializeField] private Button stopButton;

    #endregion

    #region Private State

    private string   _directoryPath;
    private string   _filePath;
    private AudioClip _recordedClip;
    private float    _startTime;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        // Prepare our save folder & filepath on the main thread
        _directoryPath = Path.Combine(Application.persistentDataPath, folderName);
        if (!Directory.Exists(_directoryPath))
            Directory.CreateDirectory(_directoryPath);

        _filePath = Path.Combine(_directoryPath, outputFileName);

        // Initial UI state
        startButton.gameObject.SetActive(true);
        stopButton .gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        startButton.onClick.AddListener(OnStartClicked);
        stopButton .onClick.AddListener(OnStopClicked);
    }

    private void OnDisable()
    {
        startButton.onClick.RemoveListener(OnStartClicked);
        stopButton .onClick.RemoveListener(OnStopClicked);
    }

    #endregion

    #region UI Callbacks

    private void OnStartClicked()
    {
        StartRecording();
        startButton.gameObject.SetActive(false);
        stopButton .gameObject.SetActive(true);
    }

    private void OnStopClicked()
    {
        StopRecording();
        stopButton .gameObject.SetActive(false);
        startButton.gameObject.SetActive(true);
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

        // Pick the device
        string device = string.IsNullOrEmpty(micDevice)
            ? Microphone.devices[0]
            : micDevice;

        // Begin recording
        _recordedClip = Microphone.Start(device, 
            loop: false, 
            lengthSec: maxLengthSec, 
            frequency: sampleRate);

        _startTime = Time.realtimeSinceStartup;
        Debug.Log($"Recording started on '{device}'");
    }

    public void StopRecording()
    {
        if (_recordedClip == null)
            return;

        // End the mic capture
        Microphone.End(null);

        // Determine actual recorded length
        float recordedSeconds = Time.realtimeSinceStartup - _startTime;

        // Trim the clip to that exact length
        var trimmed = TrimClip(_recordedClip, recordedSeconds);

        // Save via your existing WavUtility
        WavUtility.Save(_filePath, trimmed);
        Debug.Log($"Recording saved to: {_filePath}");

        _recordedClip = null;
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