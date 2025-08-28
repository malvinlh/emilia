using System;
using SQLite;

namespace EMILIA.Data
{
    /// <summary>
    /// Represents a user in the system.
    /// Maps to the "users" table in SQLite.
    /// </summary>
    [Table("users")]
    public class User
    {
        /// <summary>
        /// Primary key for the user record.
        /// </summary>
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Full name of the user.
        /// </summary>
        [Column("name")]
        public string Name { get; set; }

        /// <summary>
        /// Unique username of the user.
        /// </summary>
        [Column("username")]
        public string Username { get; set; }

        /// <summary>
        /// Date and time when the user was created.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents a conversation session between the system and a user.
    /// Maps to the "conversations" table in SQLite.
    /// </summary>
    [Table("conversations")]
    public class Conversation
    {
        /// <summary>
        /// Primary key for the conversation record.
        /// </summary>
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Foreign key linking the conversation to a user.
        /// </summary>
        [Indexed, Column("user_id")]
        public string UserId { get; set; }

        /// <summary>
        /// Date and time when the conversation started.
        /// </summary>
        [Column("started_at")]
        public DateTime StartedAt { get; set; }

        /// <summary>
        /// Date and time when the conversation ended (nullable).
        /// </summary>
        [Column("ended_at")]
        public DateTime? EndedAt { get; set; }

        /// <summary>
        /// Optional title or subject of the conversation.
        /// </summary>
        [Column("title")]
        public string Title { get; set; }
    }

    /// <summary>
    /// Represents a single message in a conversation.
    /// Maps to the "messages" table in SQLite.
    /// </summary>
    [Table("messages")]
    public class Message
    {
        /// <summary>
        /// Primary key for the message record.
        /// </summary>
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Foreign key linking the message to a conversation.
        /// </summary>
        [Indexed, Column("conversation_id")]
        public string ConversationId { get; set; }

        /// <summary>
        /// Sender of the message (e.g., "user" or "system").
        /// </summary>
        [Column("sender")]
        public string Sender { get; set; }

        /// <summary>
        /// The actual text content of the message.
        /// Maps to the "message" column in SQLite.
        /// </summary>
        [Column("message")]
        public string Text { get; set; }

        /// <summary>
        /// Date and time when the message was sent.
        /// </summary>
        [Column("sent_at")]
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// Represents a journal entry created by a user.
    /// Maps to the "journals" table in SQLite.
    /// </summary>
    [Table("journals")]
    public class Journal
    {
        /// <summary>
        /// Primary key for the journal record.
        /// </summary>
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Foreign key linking the journal to a user.
        /// </summary>
        [Indexed, Column("user_id")]
        public string UserId { get; set; }

        /// <summary>
        /// Title of the journal entry.
        /// </summary>
        [Column("title")]
        public string Title { get; set; }

        /// <summary>
        /// Main content of the journal entry.
        /// </summary>
        [Column("content")]
        public string Content { get; set; }

        /// <summary>
        /// Date and time when the journal was created.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Date and time when the journal was last updated.
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Represents a summary of a conversation.
    /// Maps to the "summary" table in SQLite.
    /// </summary>
    [Table("summary")]
    public class Summary
    {
        /// <summary>
        /// Primary key for the summary record.
        /// </summary>
        [PrimaryKey, Column("id")]
        public string Id { get; set; }

        /// <summary>
        /// Foreign key linking the summary to a conversation.
        /// </summary>
        [Indexed, Column("conversation_id")]
        public string ConversationId { get; set; }

        /// <summary>
        /// The generated summary text of the conversation.
        /// </summary>
        [Column("summary_text")]
        public string SummaryText { get; set; }

        /// <summary>
        /// Date and time when the summary was created.
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}