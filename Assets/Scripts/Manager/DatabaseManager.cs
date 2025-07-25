// DatabaseManager.cs
using UnityEngine;
using SQLite;
using System.IO;
using EMILIA.Data;   // ← make sure this matches the namespace in your DatabaseModels.cs

namespace EMILIA.Data
{
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager Instance { get; private set; }

        // Expose the raw SQLiteConnection so your Local*Service classes can use it
        public SQLiteConnection DB => _db;
        private SQLiteConnection _db;

        private void Awake()
        {
            // — Singleton pattern —
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            // 1) Build path under persistentDataPath
            const string dbFileName = "emilia.db";
            var dbPath = Path.Combine(Application.persistentDataPath, dbFileName);
            Debug.Log($"[DatabaseManager] Opening SQLite DB at: {dbPath}");

            // 2) Open (creates file if missing)
            _db = new SQLiteConnection(dbPath);

            // 3) Enforce foreign‐key constraints
            _db.Execute("PRAGMA foreign_keys = ON;");

            // 4) Create tables for all your DataModel classes
            CreateTables();
        }

        private void CreateTables()
        {
            _db.CreateTable<User>();         // maps to "users"
            _db.CreateTable<Conversation>(); // maps to "conversations"
            _db.CreateTable<Message>();      // maps to "messages"
            _db.CreateTable<Journal>();      // maps to "journals"
            _db.CreateTable<Summary>();      // maps to "summary"

            Debug.Log("[DatabaseManager] Tables ensured: users, conversations, messages, journals, summary");
        }

        private void OnApplicationQuit()
        {
            // Cleanly close the DB (optional)
            _db?.Close();
            _db = null;
        }
    }
}