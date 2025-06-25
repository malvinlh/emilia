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
    [Tooltip("Image background untuk mengganti warna (optional)")]
    [SerializeField] private Image backgroundImage;
    [Tooltip("Image tambahan, misal icon, yang juga akan di-tint (optional)")]
    [SerializeField] private Image extraImage;
    [Tooltip("TextMeshPro label untuk mengganti warna (optional)")]
    [SerializeField] private TextMeshProUGUI labelText;

    [Header("Background Colors")]
    [SerializeField] private Color normalBg   = new Color(0,0,0,0);
    [SerializeField] private Color hoverBg    = new Color(0.6f,0.5f,0.9f,1f);
    [SerializeField] private Color pressedBg  = new Color(0.5f,0.4f,0.8f,1f);
    [SerializeField] private Color disabledBg = new Color(0,0,0,0.2f);

    [Header("Extra Image Colors")]
    [SerializeField] private Color normalExtra   = Color.white;
    [SerializeField] private Color hoverExtra    = Color.white;
    [SerializeField] private Color pressedExtra  = Color.white;
    [SerializeField] private Color disabledExtra = Color.gray;

    [Header("Text Colors")]
    [SerializeField] private Color normalText   = Color.black;
    [SerializeField] private Color hoverText    = Color.white;
    [SerializeField] private Color pressedText  = Color.white;
    [SerializeField] private Color disabledText = Color.gray;

    private Button _button;

    private void Reset()
    {
        backgroundImage = GetComponent<Image>();
        labelText       = GetComponentInChildren<TextMeshProUGUI>();
        // extraImage biarkan kosong kalau tidak ada
    }

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (labelText == null)
            labelText = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
        // extraImage tetap optional

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
        // 1) Tint backgroundImage
        if (backgroundImage != null)
        {
            switch (s)
            {
                case State.Normal:   backgroundImage.color = normalBg;   break;
                case State.Hover:    backgroundImage.color = hoverBg;    break;
                case State.Pressed:  backgroundImage.color = pressedBg;  break;
                case State.Disabled: backgroundImage.color = disabledBg; break;
            }
        }

        // 2) Tint extraImage dengan warna khusus extra
        if (extraImage != null)
        {
            switch (s)
            {
                case State.Normal:   extraImage.color = normalExtra;   break;
                case State.Hover:    extraImage.color = hoverExtra;    break;
                case State.Pressed:  extraImage.color = pressedExtra;  break;
                case State.Disabled: extraImage.color = disabledExtra; break;
            }
        }

        // 3) Tint labelText
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