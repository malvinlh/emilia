using UnityEngine;
using TMPro;

public class TextBlink : MonoBehaviour
{
    public TextMeshProUGUI targetText; // drag TextMeshProUGUI object here
    public float blinkSpeed = 2f; // how fast it blinks (higher = faster)

    private Color originalColor;

    void Start()
    {
        if (targetText != null)
            originalColor = targetText.color;
    }

    void Update()
    {
        if (targetText == null) return;

        // Smooth alpha oscillation between 0 and 1
        float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;

        Color newColor = originalColor;
        newColor.a = alpha;
        targetText.color = newColor;
    }
}