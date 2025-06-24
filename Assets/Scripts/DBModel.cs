// [System.Serializable]
// public class User
// {
//     public string id;
//     public string name;
//     public string username;
//     public string created_at;
// }

// [System.Serializable]
// public class Journal
// {
//     public int id;
//     public string user_id;
//     public string title;
//     public string content;
//     public string created_at;
//     public string updated_at;
//     public User users;      // Supabase akan mereturn objek users jika kamu embed relasi
// }

// [System.Serializable]
// public class JournalList
// {
//     public Journal[] data;
// }