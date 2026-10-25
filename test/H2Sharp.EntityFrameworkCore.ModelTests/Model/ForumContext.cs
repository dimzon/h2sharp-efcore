namespace H2Sharp.EntityFrameworkCore.ModelTests.Model;

/// <summary>
///     The forum: the model and the way it is stored.
/// </summary>
/// <remarks>
///     <para>
///         The names are the ones H2 folds to upper case, and the provider quotes them, so a
///         table is called "Forums" and a column "UserName" in the database and in the model
///         alike.
///     </para>
///     <para>
///         The relations are joined by their columns: the forum keeps no navigations, so its
///         queries name the joins themselves. The port reads a foreign key inside CREATE TABLE
///         as well, and ForumRelationshipExamples asks the navigations on a model of its own.
///     </para>
/// </remarks>
public sealed class ForumContext : DbContext
{
    public ForumContext(DbContextOptions<ForumContext> options)
        : base(options)
    {
    }

    public DbSet<Forum> Forums { get; set; } = null!;

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<Topic> Topics { get; set; } = null!;

    public DbSet<Post> Posts { get; set; } = null!;

    public DbSet<Tag> Tags { get; set; } = null!;

    public DbSet<TopicTag> TopicTags { get; set; } = null!;

    public DbSet<Vote> Votes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Forum>(forum =>
        {
            forum.ToTable("Forums");
            forum.HasKey(f => f.Id);
            forum.Property(f => f.Id).UseIdentityColumn();
            forum.Property(f => f.Name).HasMaxLength(100).IsRequired();
            forum.Property(f => f.Description).HasMaxLength(500).IsRequired();
            forum.HasIndex(f => f.Name).IsUnique();
        });

        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("Users");
            user.HasKey(u => u.Id);
            user.Property(u => u.Id).UseIdentityColumn();
            user.Property(u => u.UserName).HasMaxLength(50).IsRequired();
            user.Property(u => u.Email).HasMaxLength(200).IsRequired();
            user.Property(u => u.Balance).HasPrecision(18, 2);
            user.Property(u => u.Role).HasConversion<string>();
            user.HasIndex(u => u.UserName).IsUnique();
            user.HasIndex(u => u.LastSeenAt);
        });

        modelBuilder.Entity<Topic>(topic =>
        {
            topic.ToTable("Topics");
            topic.HasKey(t => t.Id);
            topic.Property(t => t.Id).UseIdentityColumn();
            topic.Property(t => t.Title).HasMaxLength(200).IsRequired();
            topic.Property(t => t.Body).IsRequired();
            topic.Property(t => t.ModeratorNote).HasMaxLength(500);
            topic.HasIndex(t => t.LastPostAt);
            topic.HasIndex(t => t.ForumId);
        });

        modelBuilder.Entity<Post>(post =>
        {
            post.ToTable("Posts");
            post.HasKey(p => p.Id);
            post.Property(p => p.Id).UseIdentityColumn();
            post.Property(p => p.Body).IsRequired();
            post.HasIndex(p => p.CreatedAt);
            post.HasIndex(p => p.TopicId);
        });

        modelBuilder.Entity<Tag>(tag =>
        {
            tag.ToTable("Tags");
            tag.HasKey(t => t.Id);
            tag.Property(t => t.Id).UseIdentityColumn();
            tag.Property(t => t.Name).HasMaxLength(50).IsRequired();
            tag.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<TopicTag>(topicTag =>
        {
            topicTag.ToTable("TopicTags");
            topicTag.HasKey(tt => new { tt.TopicId, tt.TagId });
        });

        modelBuilder.Entity<Vote>(vote =>
        {
            vote.ToTable("Votes");
            vote.HasKey(v => v.Id);
            vote.Property(v => v.Id).UseIdentityColumn();
            vote.HasIndex(v => new { v.PostId, v.UserId }).IsUnique();
        });
    }
}
