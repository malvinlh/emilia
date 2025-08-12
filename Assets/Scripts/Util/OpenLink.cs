using UnityEngine;

public class OpenLink : MonoBehaviour
{
    [SerializeField] private string url = "https://example.com";

    public void OpenExternalLink()
    {
        Application.OpenURL(url);
    }
}