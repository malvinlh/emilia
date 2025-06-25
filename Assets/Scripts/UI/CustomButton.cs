using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button), typeof(Image))]
public class CustomButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler,   IPointerUpHandler
{
    [Header("UI References")]
    [Tooltip("Image background untuk mengganti warna")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("TextMeshPro label untuk mengganti warna (optional)")]
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Background Colors")]
    [SerializeField] private Color normalBg   = new Color(0,0,0,0);           // transparan
    [SerializeField] private Color hoverBg    = new Color(0.6f,0.5f,0.9f,1f); // ungu
    [SerializeField] private Color pressedBg  = new Color(0.5f,0.4f,0.8f,1f); // ungu gelap
    [SerializeField] private Color disabledBg = new Color(0,0,0,0.2f);       // semi‐transparan

    [Header("Text Colors")]
    [SerializeField] private Color normalText   = Color.black;
    [SerializeField] private Color hoverText    = Color.white;
    [SerializeField] private Color pressedText  = Color.white;
    [SerializeField] private Color disabledText = Color.gray;

    private Button _button;

    private void Reset()
    {
        // Auto‐assign kalau lupa drag di Inspector
        backgroundImage = GetComponent<Image>();
        labelText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        // hanya cari label kalau ada child TMP
        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
        
        // inisialisasi ke state awal
        ApplyState(_button.interactable ? State.Normal : State.Disabled);
    }

    private void OnEnable()
    {
        ApplyState(_button.interactable ? State.Normal : State.Disabled);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_button.interactable) return;
        ApplyState(State.Hover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!_button.interactable) return;
        ApplyState(State.Normal);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!_button.interactable) return;
        ApplyState(State.Pressed);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_button.interactable) return;
        ApplyState(State.Hover);
    }

    private enum State { Normal, Hover, Pressed, Disabled }

    private void ApplyState(State s)
    {
        // ubah background
        switch (s)
        {
            case State.Normal:   backgroundImage.color = normalBg;   break;
            case State.Hover:    backgroundImage.color = hoverBg;    break;
            case State.Pressed:  backgroundImage.color = pressedBg;  break;
            case State.Disabled: backgroundImage.color = disabledBg; break;
        }
        // ubah teks hanya jika ada labelText
        if (labelText != null)
        {
            switch (s)
            {
                case State.Normal:   labelText.color = normalText;   break;
                case State.Hover:    labelText.color = hoverText;    break;
                case State.Pressed:  labelText.color = pressedText;  break;
                case State.Disabled: labelText.color = disabledText; break;
            }
        }
    }
}