using H2Sharp.EntityFrameworkCore.ModelTests.Model;

namespace H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

/// <summary>
///     The data of the forum: the same rows in every run.
/// </summary>
/// <remarks>
///     <para>
///         The dates are written out, not counted from the clock: a query that says "the topics of
///         the last week" has to mean the same thing today and tomorrow.
///     </para>
///     <para>
///         The ids are not written: the identity column of the engine gives them out, and the
///         tests read the rows by what is in them and not by their number.
///     </para>
/// </remarks>
public static class ForumData
{
    /// <summary>
    ///     The moment the forum begins: everything the seed puts in is after it.
    /// </summary>
    public static readonly DateTime Start = new(2024, 1, 1, 9, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    ///     The moment the seed ends: everything the seed puts in is before it.
    /// </summary>
    public static readonly DateTime Now = new(2024, 6, 1, 12, 0, 0, DateTimeKind.Unspecified);

    public static List<Forum> Forums() =>
    [
        new Forum
        {
            Name = "Programming",
            Description = "Languages, compilers and the tools around them.",
            CreatedAt = Start.AddDays(1),
            IsArchived = false,
        },
        new Forum
        {
            Name = "Databases",
            Description = "Stores, queries and the engines behind them.",
            CreatedAt = Start.AddDays(2),
            IsArchived = false,
        },
        new Forum
        {
            Name = "Old announcements",
            Description = "What the forum said long ago.",
            CreatedAt = Start.AddDays(3),
            IsArchived = true,
        },
    ];

    public static List<User> Users() =>
    [
        new User
        {
            UserName = "ada",
            Email = "ada@example.org",
            JoinedAt = Start.AddDays(5),
            LastSeenAt = Now.AddHours(-2),
            BirthDate = new DateOnly(1990, 4, 12),
            PreferredTime = new TimeOnly(9, 30),
            Reputation = 420,
            Balance = 12.50m,
            IsBanned = false,
            Role = UserRole.Administrator,
            Avatar = [1, 2, 3],
        },
        new User
        {
            UserName = "grace",
            Email = "grace@example.org",
            JoinedAt = Start.AddDays(8),
            LastSeenAt = Now.AddDays(-1),
            BirthDate = new DateOnly(1985, 12, 1),
            PreferredTime = new TimeOnly(18, 0),
            Reputation = 380,
            Balance = 3.00m,
            IsBanned = false,
            Role = UserRole.Moderator,
            Avatar = [],
        },
        new User
        {
            UserName = "linus",
            Email = "linus@example.org",
            JoinedAt = Start.AddDays(12),
            LastSeenAt = Now.AddDays(-3),
            BirthDate = null,
            PreferredTime = null,
            Reputation = 250,
            Balance = 0.25m,
            IsBanned = false,
            Role = UserRole.Member,
            Avatar = [9],
        },
        new User
        {
            UserName = "margaret",
            Email = "margaret@example.org",
            JoinedAt = Start.AddDays(20),
            LastSeenAt = Now.AddDays(-45),
            BirthDate = new DateOnly(1992, 6, 30),
            PreferredTime = new TimeOnly(7, 15),
            Reputation = 90,
            Balance = 7.75m,
            IsBanned = false,
            Role = UserRole.Member,
            Avatar = [],
        },
        new User
        {
            UserName = "trouble",
            Email = "trouble@example.org",
            JoinedAt = Start.AddDays(30),
            LastSeenAt = Now.AddDays(-90),
            BirthDate = null,
            PreferredTime = null,
            Reputation = 0,
            Balance = 0m,
            IsBanned = true,
            Role = UserRole.Guest,
            Avatar = [],
        },
    ];

    public static List<Topic> Topics() =>
    [
        new Topic
        {
            ForumId = 1,
            AuthorId = 1,
            Title = "What a compiler does with the source",
            Body = "A walk through the phases, from the characters to the code.",
            CreatedAt = Start.AddDays(10),
            LastPostAt = Now.AddHours(-3),
            ViewCount = 1200,
            IsPinned = true,
            IsClosed = false,
            ModeratorNote = null,
        },
        new Topic
        {
            ForumId = 1,
            AuthorId = 2,
            Title = "Types without a runtime",
            Body = "What a static type system can and cannot promise.",
            CreatedAt = Start.AddDays(20),
            LastPostAt = Now.AddHours(-30),
            ViewCount = 640,
            IsPinned = false,
            IsClosed = false,
            ModeratorNote = null,
        },
        new Topic
        {
            ForumId = 1,
            AuthorId = 3,
            Title = "The cost of an abstraction",
            Body = "Why the layer costs and what it buys.",
            CreatedAt = Start.AddDays(40),
            LastPostAt = Now.AddDays(-2),
            ViewCount = 300,
            IsPinned = false,
            IsClosed = true,
            ModeratorNote = "Locked: the thread turned into a flame war.",
        },
        new Topic
        {
            ForumId = 2,
            AuthorId = 2,
            Title = "Reading a query plan",
            Body = "What the plan says and in which order it is read.",
            CreatedAt = Start.AddDays(15),
            LastPostAt = Now.AddHours(-10),
            ViewCount = 890,
            IsPinned = false,
            IsClosed = false,
            ModeratorNote = null,
        },
        new Topic
        {
            ForumId = 2,
            AuthorId = 4,
            Title = "Indexes that are never used",
            Body = "An index is a promise; a promise that is not kept costs twice.",
            CreatedAt = Start.AddDays(50),
            LastPostAt = Now.AddDays(-6),
            ViewCount = 150,
            IsPinned = false,
            IsClosed = false,
            ModeratorNote = null,
        },
        new Topic
        {
            ForumId = 2,
            AuthorId = 1,
            Title = "Transactions and the order of the writes",
            Body = "What a transaction guarantees, and in which order.",
            CreatedAt = Start.AddDays(25),
            LastPostAt = Now.AddDays(-1),
            ViewCount = 420,
            IsPinned = false,
            IsClosed = false,
            ModeratorNote = null,
        },
        new Topic
        {
            ForumId = 3,
            AuthorId = 3,
            Title = "The forum moves",
            Body = "The address changes and the old one dies.",
            CreatedAt = Start.AddDays(60),
            LastPostAt = Now.AddDays(-120),
            ViewCount = 80,
            IsPinned = false,
            IsClosed = true,
            ModeratorNote = "The announcement is kept for the history.",
        },
    ];

    public static List<Post> Posts() =>
    [
        new Post { TopicId = 1, AuthorId = 1, Body = "The scanner first.", CreatedAt = Start.AddDays(10).AddHours(1), IsHidden = false },
        new Post { TopicId = 1, AuthorId = 2, Body = "The parser builds the tree.", CreatedAt = Start.AddDays(10).AddHours(2), IsHidden = false },
        new Post { TopicId = 1, AuthorId = 3, Body = "The tree is walked and the code is written.", CreatedAt = Now.AddHours(-3), IsHidden = false },
        new Post { TopicId = 1, AuthorId = 4, Body = "And the optimizer has its say.", CreatedAt = Now.AddHours(-2), IsHidden = false },
        new Post { TopicId = 2, AuthorId = 2, Body = "A promise the compiler keeps.", CreatedAt = Start.AddDays(20), IsHidden = false },
        new Post { TopicId = 2, AuthorId = 1, Body = "And the promises it does not.", CreatedAt = Now.AddHours(-30), IsHidden = false },
        new Post { TopicId = 3, AuthorId = 3, Body = "The abstraction is a cost.", CreatedAt = Start.AddDays(40), IsHidden = false },
        new Post { TopicId = 4, AuthorId = 2, Body = "The plan is read from the leaves.", CreatedAt = Start.AddDays(15), IsHidden = false },
        new Post { TopicId = 4, AuthorId = 3, Body = "The order of the nodes is the order of the work.", CreatedAt = Now.AddHours(-10), IsHidden = false },
        new Post { TopicId = 5, AuthorId = 4, Body = "The index is written and never read.", CreatedAt = Start.AddDays(50), IsHidden = false },
        new Post { TopicId = 5, AuthorId = 1, Body = "The promise is kept in the statistics, not in the index.", CreatedAt = Now.AddDays(-6), IsHidden = false },
        new Post { TopicId = 6, AuthorId = 1, Body = "The writes come in the order of the locks.", CreatedAt = Start.AddDays(25), IsHidden = false },
        new Post { TopicId = 6, AuthorId = 2, Body = "And the commit makes them one.", CreatedAt = Now.AddDays(-1), IsHidden = false },
        new Post { TopicId = 7, AuthorId = 3, Body = "The old address is dead.", CreatedAt = Start.AddDays(60), IsHidden = true },
    ];

    public static List<Tag> Tags() =>
    [
        new Tag { Name = "compilers" },
        new Tag { Name = "types" },
        new Tag { Name = "queries" },
        new Tag { Name = "indexes" },
    ];

    public static List<TopicTag> TopicTags() =>
    [
        new TopicTag { TopicId = 1, TagId = 1 },
        new TopicTag { TopicId = 1, TagId = 2 },
        new TopicTag { TopicId = 2, TagId = 2 },
        new TopicTag { TopicId = 3, TagId = 1 },
        new TopicTag { TopicId = 4, TagId = 3 },
        new TopicTag { TopicId = 5, TagId = 4 },
        new TopicTag { TopicId = 6, TagId = 3 },
    ];

    public static List<Vote> Votes() =>
    [
        new Vote { PostId = 3, UserId = 1, Value = 1, CreatedAt = Now.AddHours(-2) },
        new Vote { PostId = 3, UserId = 2, Value = 1, CreatedAt = Now.AddHours(-2) },
        new Vote { PostId = 4, UserId = 3, Value = 1, CreatedAt = Now.AddHours(-1) },
        new Vote { PostId = 4, UserId = 4, Value = -1, CreatedAt = Now.AddHours(-1) },
        new Vote { PostId = 13, UserId = 1, Value = 1, CreatedAt = Now.AddDays(-1) },
    ];
}
