using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class RecordAudio : MonoBehaviour
{
    #region Inspector

    [Header("Recording Settings")]
    [SerializeField] private string outputFileName = "recording.wav";
    [SerializeField] private string folderName     = "Recordings";
    [SerializeField] private string micDevice      = "";
    [SerializeField] private int    sampleRate     = 44100;
    [SerializeField] private int    maxLengthSec   = 3599;

    [Header("Mic UI (Single Button)")]
    [SerializeField] private Button      micButton;        // tombol mic tunggal (toggle)
    [SerializeField] private CanvasGroup micCanvasGroup;   // taruh di object tombol mic (atau parent)
    [SerializeField] private float       blinkMinAlpha = 0.45f;
    [SerializeField] private float       blinkMaxAlpha = 1.0f;
    [SerializeField] private float       blinkSpeed    = 2.0f; // semakin besar, semakin cepat kedip

    #endregion

    #region Events & State

    public event Action<string> OnSaved;
    public event Action<bool>   OnMicStateChanged;

    public bool IsRecording { get; private set; }

    private string    _directoryPath;
    private string    _filePath;
    private AudioClip _recordedClip;
    private float     _startTime;
    private Coroutine _blinkCo;

    #endregion

    #region Unity

    private void Awake()
    {
        // Siapkan folder simpan
        string targetFolder = @"D:\Emilia\AI\Recordings";
        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        _directoryPath = targetFolder;
        _filePath      = Path.Combine(_directoryPath, outputFileName);

        if (micButton != null)  micButton.onClick.AddListener(OnMicClicked);
        if (micCanvasGroup == null && micButton != null)
            micCanvasGroup = micButton.GetComponent<CanvasGroup>();

        // State awal: mic terlihat (active) & tidak blinking
        ApplyMicVisibility(true);
        ApplyMicInteractable(true);
        ApplyBlink(false);
    }

    private void OnDestroy()
    {
        if (micButton != null) micButton.onClick.RemoveListener(OnMicClicked);
    }

    #endregion

    #region Public API (dipanggil ChatManager)

    /// <summary>
    /// Sinkron UI mic:
    /// - isOn: status rekaman saat ini (untuk kontrol blinking)
    /// - uiEnabled: kalau terlihat, boleh diklik atau tidak
    /// - forceHide: paksa sembunyikan (GameObject.SetActive(false))
    /// </summary>
    public void SetUIState(bool isOn, bool uiEnabled, bool forceHide, bool waitingForAI = false)
    {
        if (forceHide)
        {
            // Disembunyikan total (misalnya saat input field sedang ada teks)
            ApplyBlink(false);
            ApplyMicVisibility(false);
            return;
        }

        // Tampilkan mic
        ApplyMicVisibility(true);

        // Kalau sedang menunggu AI → hanya disable interactable, tidak hide
        if (waitingForAI)
        {
            ApplyMicInteractable(false);
            ApplyBlink(false); // matikan blinking biar user tahu ini idle
            return;
        }

        // Normal case
        ApplyMicInteractable(uiEnabled);
        ApplyBlink(isOn);
    }

    #endregion

    #region UI callbacks

    private void OnMicClicked()
    {
        if (IsRecording) StopRecording();
        else             StartRecording();
    }

    #endregion

    #region Recording

    public void StartRecording()
    {
        if (IsRecording) return;

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone devices found!");
            return;
        }

        string device = string.IsNullOrEmpty(micDevice) ? Microphone.devices[0] : micDevice;
        _recordedClip = Microphone.Start(device, loop:false, lengthSec:maxLengthSec, frequency:sampleRate);
        _startTime    = Time.realtimeSinceStartup;
        IsRecording   = true;

        OnMicStateChanged?.Invoke(true);

        // Pastikan mic terlihat & blinking saat start
        ApplyMicVisibility(true);
        ApplyMicInteractable(true);
        ApplyBlink(true);

        Debug.Log($"Recording started on '{device}'");
    }

    public void StopRecording()
    {
        if (!IsRecording) return;

        Microphone.End(null);
        IsRecording = false;

        float recordedSeconds = Time.realtimeSinceStartup - _startTime;
        var trimmed = TrimClip(_recordedClip, recordedSeconds);

        WavUtility.Save(_filePath, trimmed);
        Debug.Log($"Recording saved to: {_filePath}");

        _recordedClip = null;

        OnMicStateChanged?.Invoke(false);

        // Hentikan blinking (kembali alpha penuh). Visibility/interactable akan diatur ChatManager.
        ApplyBlink(false);
        SetCanvasAlpha(1f);

        // Beritahu ChatManager untuk upload/transcribe & mengatur UI berikutnya
        OnSaved?.Invoke(_filePath);
    }

    private AudioClip TrimClip(AudioClip clip, float length)
    {
        int samples = Mathf.Min((int)(length * clip.frequency), clip.samples);
        var data = new float[samples * clip.channels];
        clip.GetData(data, 0);

        var newClip = AudioClip.Create(clip.name, samples, clip.channels, clip.frequency, false);
        newClip.SetData(data, 0);
        return newClip;
    }

    #endregion

    #region Visual (Blink tanpa Animator)

    private void ApplyMicVisibility(bool visible)
    {
        if (micButton != null)
            micButton.gameObject.SetActive(visible);
        if (!visible)
        {
            // jika disembunyikan, pastikan blink mati & alpha reset
            ApplyBlink(false);
            SetCanvasAlpha(1f);
        }
    }

    private void ApplyMicInteractable(bool interactable)
    {
        if (micButton != null)
            micButton.interactable = interactable;
    }

    private void ApplyBlink(bool shouldBlink)
    {
        if (shouldBlink) StartBlink();
        else             StopBlink();
    }

    private void StartBlink()
    {
        if (micCanvasGroup == null) return;
        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = StartCoroutine(CoBlink());
    }

    private void StopBlink()
    {
        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = null;
        SetCanvasAlpha(1f);
    }

    private System.Collections.IEnumerator CoBlink()
    {
        // Kedip mulus: alpha = Lerp(min, max, 0.5 + 0.5*sin(t*speed))
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * blinkSpeed;
            float s = 0.5f + 0.5f * Mathf.Sin(t);
            float a = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, s);
            SetCanvasAlpha(a);
            yield return null;
        }
    }

    private void SetCanvasAlpha(float a)
    {
        if (micCanvasGroup != null)
        {
            micCanvasGroup.alpha = a;
        }
        else if (micButton != null)
        {
            // fallback: ubah warna Image utama
            var img = micButton.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                c.a = a;
                img.color = c;
            }
        }
    }

    #endregion
}