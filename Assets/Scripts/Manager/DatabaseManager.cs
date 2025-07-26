using System.IO;
using UnityEngine;
using SQLite;

namespace EMILIA.Data
{
    public class DatabaseManager : MonoBehaviour
    {
        private const string DatabaseFileName = "emilia.db";

        #region Singleton

        private static DatabaseManager _instance;
        /// <summary>
        /// Singleton instance of the DatabaseManager.
        /// </summary>
        public static DatabaseManager Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        #endregion

        #region Database Connection

        /// <summary>
        /// The shared SQLiteConnection. All CRUD services should use this.
        /// </summary>
        public SQLiteConnection DB => _db;
        private SQLiteConnection _db;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            SetupSingleton();
            InitializeDatabase();
        }

        private void OnApplicationQuit()
        {
            CloseDatabase();
        }

        #endregion

        #region Initialization

        private void SetupSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void InitializeDatabase()
        {
            var path = GetDatabasePath();
            Debug.Log($"[DatabaseManager] Opening SQLite DB at: {path}");

            _db = CreateConnection(path);
            EnableForeignKeys(_db);
            CreateTables();
        }

        #endregion

        #region Database Helpers

        private static string GetDatabasePath()
        {
            return Path.Combine(Application.persistentDataPath, DatabaseFileName);
        }

        private static SQLiteConnection CreateConnection(string path)
        {
            var connString = new SQLiteConnectionString(
                databasePath: path,
                storeDateTimeAsTicks: false, 
                openFlags: SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create
            );
            return new SQLiteConnection(connString);
        }

        private static void EnableForeignKeys(SQLiteConnection connection)
        {
            connection.Execute("PRAGMA foreign_keys = ON;");
        }

        private void CreateTables()
        {
            _db.CreateTable<User>();
            _db.CreateTable<Conversation>();
            _db.CreateTable<Message>();
            _db.CreateTable<Journal>();
            _db.CreateTable<Summary>();

            Debug.Log("[DatabaseManager] Tables ensured: users, conversations, messages, journals, summary");
        }

        private void CloseDatabase()
        {
            _db?.Close();
            _db = null;
        }

        #endregion
    }
}