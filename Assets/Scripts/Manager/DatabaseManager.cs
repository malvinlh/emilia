// Assets/Scripts/Data/DatabaseManager.cs
using UnityEngine;
using SQLite;
using System.IO;

namespace EMILIA.Data
{
    public class DatabaseManager : MonoBehaviour
    {
        public static DatabaseManager Instance { get; private set; }

        /// <summary>
        /// The shared SQLiteConnection. All CRUD services should use this.
        /// </summary>
        public SQLiteConnection DB => _db;
        private SQLiteConnection _db;

        private void Awake()
        {
            // Singleton pattern
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
            // 1) Build full path under persistentDataPath
            const string filename = "emilia.db";
            var path = Path.Combine(Application.persistentDataPath, filename);
            Debug.Log($"[DatabaseManager] Opening SQLite DB at: {path}");

            // 2) Open (or create) with ISO8601 DateTime storage
            var connString = new SQLiteConnectionString(
                databasePath: path,
                storeDateTimeAsTicks: false, // <-- store DateTime as "YYYY-MM-DDTHH:MM:SS"
                openFlags: SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create
            );
            _db = new SQLiteConnection(connString);

            // 3) Turn on foreign-key support (for cascades, FK integrity)
            _db.Execute("PRAGMA foreign_keys = ON;");

            // 4) Create tables to match your ER schema
            _db.CreateTable<User>();         // users(id TEXT PK, name TEXT, username TEXT, created_at TEXT)
            _db.CreateTable<Conversation>(); // conversations(id TEXT PK, user_id TEXT, started_at TEXT, ended_at TEXT)
            _db.CreateTable<Message>();      // messages(id TEXT PK, conversation_id TEXT, sender TEXT, message TEXT, sent_at TEXT)
            _db.CreateTable<Journal>();      // journals(id TEXT PK, user_id TEXT, title TEXT, content TEXT, created_at TEXT, updated_at TEXT)
            _db.CreateTable<Summary>();      // summary(id TEXT PK, conversation_id TEXT, summary_text TEXT, created_at TEXT)

            Debug.Log("[DatabaseManager] Tables ensured: users, conversations, messages, journals, summary");
        }

        private void OnApplicationQuit()
        {
            // Cleanly close the DB
            _db?.Close();
            _db = null;
        }
    }
}