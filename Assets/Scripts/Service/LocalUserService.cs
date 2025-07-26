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
            var existing = _db.Find<User>(nickname);

            var normalizedFullName = 
                string.IsNullOrWhiteSpace(fullName) 
                    ? null 
                    : fullName;

            if (existing != null 
            && existing.Username != null 
            && existing.Username != normalizedFullName)
            {
                onError?.Invoke(
                    "Masukkan nama lengkap yang sama dengan yang Anda gunakan saat pertama kali mendaftar."
                );
                yield break;
            }

            if (existing == null)
            {
                var user = new User {
                    Id        = nickname,
                    Name      = nickname,
                    Username  = normalizedFullName,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Insert(user);
            }
            else
            {
                existing.Username = normalizedFullName;
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