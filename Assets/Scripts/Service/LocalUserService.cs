using System;
using System.Collections;
using SQLite;
using UnityEngine;
using EMILIA.Data;

public class LocalUserService : MonoBehaviour
{
    #region Dependencies

    private SQLiteConnection _database;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        _database = DatabaseManager.Instance.DB;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Inserts a new user or updates an existing one, keyed by nickname.
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
            // Normalize the fullName: empty or whitespace -> null
            var normalizedFullName = 
                string.IsNullOrWhiteSpace(fullName) 
                    ? null 
                    : fullName;

            // Check for existing record
            var existingUser = _database.Find<User>(nickname);
            if (existingUser == null)
            {
                CreateUser(nickname, normalizedFullName);
            }
            else
            {
                UpdateUser(existingUser, normalizedFullName);
            }

            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }

        yield break;
    }

    #endregion

    #region Helpers

    private void CreateUser(string nickname, string normalizedFullName)
    {
        var newUser = new User
        {
            Id        = nickname,
            Name      = nickname,
            Username  = normalizedFullName,
            CreatedAt = DateTime.UtcNow
        };
        _database.Insert(newUser);
    }

    private void UpdateUser(User existingUser, string normalizedFullName)
    {
        existingUser.Username = normalizedFullName;
        _database.Update(existingUser);
    }

    #endregion
}