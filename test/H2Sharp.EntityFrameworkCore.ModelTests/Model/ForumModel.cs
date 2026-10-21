namespace H2Sharp.EntityFrameworkCore.ModelTests.Model;

/// <summary>
///     A forum: the forums, the topics in them, the answers to the topics, the people who
///     write them, the tags and the votes.
/// </summary>
/// <remarks>
///     <para>
///         The model is here to carry the capabilities of the provider: the types, the nullability,
///         the identity, the enum as a string, the indexes and the ordering of the nulls. Every
///         column has a type H2 2.5 has, and every name is the name H2 folds it to.
///     </para>
///     <para>
///         The tables are joined by their columns and not by the foreign keys, because the engine
///         does not read a foreign key inside CREATE TABLE yet (the reference takes it, the engine
///         refuses it: "expected "PRIMARY""). The queries join the tables the way the forum is
///         joined, and the keys of the relations are the columns the relations are built on.
///     </para>
///     <para>
///         The dates are not read from the clock: the seed puts the same dates in every run, so
///         that the answers of the queries are the same every time.
///     </para>
/// </remarks>
public sealed class Forum
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public bool IsArchived { get; set; }
}

/// <summary>
///     A person who writes in the forum.
/// </summary>
public sealed class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = "";

    public string Email { get; set; } = "";

    public DateTime JoinedAt { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public DateOnly? BirthDate { get; set; }

    public TimeOnly? PreferredTime { get; set; }

    public int Reputation { get; set; }

    public decimal Balance { get; set; }

    public bool IsBanned { get; set; }

    public UserRole Role { get; set; }

    public byte[] Avatar { get; set; } = [];
}

/// <summary>
///     A topic of a forum.
/// </summary>
public sealed class Topic
{
    public int Id { get; set; }

    public int ForumId { get; set; }

    public int AuthorId { get; set; }

    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public DateTime LastPostAt { get; set; }

    public int ViewCount { get; set; }

    public bool IsPinned { get; set; }

    public bool IsClosed { get; set; }

    public string? ModeratorNote { get; set; }
}

/// <summary>
///     An answer to a topic.
/// </summary>
public sealed class Post
{
    public int Id { get; set; }

    public int TopicId { get; set; }

    public int AuthorId { get; set; }

    public string Body { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    public bool IsHidden { get; set; }
}

/// <summary>
///     A word the topics are marked with.
/// </summary>
public sealed class Tag
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

/// <summary>
///     A topic marked with a tag.
/// </summary>
public sealed class TopicTag
{
    public int TopicId { get; set; }

    public int TagId { get; set; }
}

/// <summary>
///     A vote of a person for an answer.
/// </summary>
public sealed class Vote
{
    public int Id { get; set; }

    public int PostId { get; set; }

    public int UserId { get; set; }

    public int Value { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     The role of a person in the forum.
/// </summary>
public enum UserRole
{
    Guest,
    Member,
    Moderator,
    Administrator,
}
