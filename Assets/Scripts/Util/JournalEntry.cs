using System;
using UnityEngine;
using UnityEngine.UI;

public class JournalEntry : MonoBehaviour
{
    [Header("Auto-find jika kosong")]
    [SerializeField] private Button editButton;    // Optional (kalau ada)
    [SerializeField] private Button deleteButton;  // Wajib ada di prefab (nama: "DeleteButton")

    private Action _onEdit;
    private Action _onDelete;

    /// <summary>
    /// Dipanggil dari JournalManager setelah Instantiate.
    /// </summary>
    public void Setup(Action onEdit, Action onDelete)
    {
        _onEdit   = onEdit;
        _onDelete = onDelete;

        if (deleteButton == null)
            deleteButton = transform.Find("DeleteButton")?.GetComponent<Button>();
        if (editButton == null)
            editButton = transform.Find("EditButton")?.GetComponent<Button>(); // opsional

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => _onDelete?.Invoke());
        }

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();
            editButton.onClick.AddListener(() => _onEdit?.Invoke());
        }
    }
}