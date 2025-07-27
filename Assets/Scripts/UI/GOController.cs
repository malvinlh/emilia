using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GOController : MonoBehaviour
{
    [Header("Sidebar Canvas")]
    [SerializeField] private GameObject sidebarCanvas;

    public void HideTrashIcons()
    {
        if (sidebarCanvas == null || !sidebarCanvas.activeSelf)
            return;

        var deletes = sidebarCanvas
            .GetComponentsInChildren<Button>(true)
            .Where(b => b.gameObject.name == "DeleteButton");

        foreach (var btn in deletes)
        {
            btn.gameObject.SetActive(false);
        }
    }
}
