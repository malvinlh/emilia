using System;
using SQLite;

namespace EMILIA.Data
{
    [Table("users")]
    public class User
    {
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("username")]
        public string Username { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    [Table("conversations")]
    public class Conversation
    {
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        [Indexed, Column("user_id")]
        public string UserId { get; set; }

        [Column("started_at")]
        public DateTime StartedAt { get; set; }

        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        [Column("title")]
        public string Title { get; set; }
    }

    [Table("messages")]
    public class Message
    {
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        [Indexed, Column("conversation_id")]
        public string ConversationId { get; set; }

        [Column("sender")]
        public string Sender { get; set; }

        // Maps to the "message" column in SQLite
        [Column("message")]
        public string Text { get; set; }

        [Column("sent_at")]
        public DateTime SentAt { get; set; }
    }

    [Table("journals")]
    public class Journal
    {
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        [Indexed, Column("user_id")]
        public string UserId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [Table("summary")]
    public class Summary
    {
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        [Indexed, Column("conversation_id")]
        public string ConversationId { get; set; }

        [Column("summary_text")]
        public string SummaryText { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
