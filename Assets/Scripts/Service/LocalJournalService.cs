using System;
using System.Collections;
using System.Linq;
using SQLite;
using UnityEngine;
using EMILIA.Data;

public class LocalJournalService : MonoBehaviour
{
    #region Dependencies

    private SQLiteConnection _db;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _db = DatabaseManager.Instance.DB;
        _db.Execute("PRAGMA foreign_keys = ON;");
    }

    #endregion

    #region Queries

    /// <summary>
    /// Retrieves all journals for a given user, ordered by creation date descending.
    /// </summary>
    public IEnumerator FetchUserJournals(
        string userId,
        Action<Journal[]> onSuccess,
        Action<string> onError = null
    )
    {
        try
        {
            var journals = _db.Table<Journal>()
                              .Where(j => j.UserId == userId)
                              .OrderByDescending(j => j.CreatedAt)
                              .ToArray();

            onSuccess(journals);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }

        yield break;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Inserts a new journal entry and returns the created object.
    /// </summary>
    public IEnumerator CreateJournal(
        string userId,
        string title,
        string content,
        string createdAtIso,
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
    /// Updates the title, content, and updated_at timestamp of an existing journal.
    /// </summary>
    public IEnumerator UpdateJournal(
        string journalId,
        string newTitle,
        string newContent,
        string newUpdatedAtIso,
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
            journal.UpdatedAt = DateTime.Parse(newUpdatedAtIso);

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
    /// Deletes a journal entry by its ID.
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

    #endregion
}