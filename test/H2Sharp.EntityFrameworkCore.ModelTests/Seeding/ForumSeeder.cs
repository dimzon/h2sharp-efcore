using H2Sharp.EntityFrameworkCore.ModelTests.Model;

namespace H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

/// <summary>
///     Puts the data of the forum into an empty database.
/// </summary>
/// <remarks>
///     Every test that asks a question about the forum starts from the same rows, so that the
///     answer is the same every time and can be written down.
/// </remarks>
public static class ForumSeeder
{
    /// <summary>
    ///     Fills the database with the forum.
    /// </summary>
    /// <param name="context">The context of the empty database.</param>
    public static void Fill(ForumContext context)
    {
        context.Forums.AddRange(ForumData.Forums());
        context.Users.AddRange(ForumData.Users());
        context.Tags.AddRange(ForumData.Tags());
        context.Topics.AddRange(ForumData.Topics());
        context.Posts.AddRange(ForumData.Posts());
        context.TopicTags.AddRange(ForumData.TopicTags());
        context.Votes.AddRange(ForumData.Votes());

        context.SaveChanges();
    }
}
