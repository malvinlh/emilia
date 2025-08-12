using UnityEngine;
using TMPro;  // Remove if you are using UnityEngine.UI.Text instead

public class CurrentTimeDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timeText; // For TMP text
    // public Text timeText; // Uncomment if using legacy UI Text instead

    void Update()
    {
        // Get current system time and format it
        string currentTime = System.DateTime.Now.ToString("hh:mm tt");

        // Assign to UI Text
        timeText.text = currentTime;
    }
}