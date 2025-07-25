// LocalUserService.cs
using UnityEngine;
using System;
using System.Collections;
using SQLite;
using EMILIA.Data;

public class LocalUserService : MonoBehaviour
{
    private SQLiteConnection _db;

    void Awake()
    {
        _db = DatabaseManager.Instance.DB;
    }

    /// <summary>
    /// INSERT or UPDATE a user based on nickname as the primary key.
    /// Mirrors the Supabase UpsertUser signature.
    /// </summary>
    public IEnumerator UpsertUser(
        string nickname,
        string fullName,
        Action onSuccess,
        Action<string> onError
    )
    {
        try
        {
            // Try to find an existing user
            var existing = _db.Find<User>(nickname);

            if (existing == null)
            {
                // New user: set created_at to now
                var user = new User
                {
                    Id        = nickname,
                    Name      = nickname,
                    Username  = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Insert(user);
            }
            else
            {
                // Update only the full name (username)
                existing.Username = string.IsNullOrWhiteSpace(fullName) ? null : fullName;
                _db.Update(existing);
            }

            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
        yield break;
    }
}