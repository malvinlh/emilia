// LocalJournalService.cs
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using SQLite;
using EMILIA.Data;

public class LocalJournalService : MonoBehaviour
{
    private SQLiteConnection _db;

    void Awake()
    {
        _db = DatabaseManager.Instance.DB;
        // (optional) enforce foreign‐keys if you need cascades:
        _db.Execute("PRAGMA foreign_keys = ON;");
    }

    /// <summary>
    /// GET all journals for this user, newest first.
    /// </summary>
    public IEnumerator FetchUserJournals(
        string userId,
        Action<Journal[]> onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            var items = _db.Table<Journal>()
                           .Where(j => j.UserId == userId)
                           .OrderByDescending(j => j.CreatedAt)
                           .ToArray();
            onSuccess(items);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// INSERT a new journal row.
    /// </summary>
    public IEnumerator CreateJournal(
        string userId,
        string title,
        string content,
        string createdAtIso,            // e.g. "2025-07-25T08:30:00"
        Action<Journal> onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            var timestamp = DateTime.Parse(createdAtIso);
            var journal = new Journal
            {
                Id        = Guid.NewGuid().ToString(),
                UserId    = userId,
                Title     = title,
                Content   = content,
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };
            _db.Insert(journal);
            onSuccess?.Invoke(journal);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// UPDATE title, content, and updated_at for an existing journal.
    /// </summary>
    public IEnumerator UpdateJournal(
        string journalId,
        string newTitle,
        string newContent,
        string newUpdatedAt,            // e.g. "2025-07-25T09:00:00"
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            var journal = _db.Find<Journal>(journalId);
            if (journal == null)
            {
                onError?.Invoke($"Journal '{journalId}' not found");
                yield break;
            }

            journal.Title     = newTitle;
            journal.Content   = newContent;
            journal.UpdatedAt = DateTime.Parse(newUpdatedAt);

            _db.Update(journal);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }

    /// <summary>
    /// DELETE a single journal by ID.
    /// </summary>
    public IEnumerator DeleteJournal(
        string journalId,
        Action onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            _db.Execute("DELETE FROM journals WHERE id = ?", journalId);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }
}