using H2Sharp.EntityFrameworkCore.ModelTests.Seeding;

namespace H2Sharp.EntityFrameworkCore.ModelTests;

/// <summary>
///     The numbers of the forum are rounded, signed and combined in the query, and both engines
///     answer the same: the functions of H2 answer what the Math methods of .NET mean.
/// </summary>
public class ForumArithmeticExamples
{
    [Fact]
    public void The_balance_is_rounded_up_down_and_toward_zero()
    {
        using var database = ForumDatabase.Filled(nameof(The_balance_is_rounded_up_down_and_toward_zero));
        using var context = database.Context();
        var source = ForumData.Users().OrderBy(u => u.Id).ToArray();

        var rounded = context.Users.OrderBy(u => u.Id).Select(u => new
        {
            Up = Math.Ceiling(u.Balance),
            Down = Math.Floor(u.Balance),
            TowardZero = Math.Truncate(u.Balance)
        });
        var expected = source.Select(u => new
        {
            Up = Math.Ceiling(u.Balance),
            Down = Math.Floor(u.Balance),
            TowardZero = Math.Truncate(u.Balance)
        }).ToArray();

        // CEILING, FLOOR and TRUNC keep the places of the decimal they take.
        Assert.Equal(expected, rounded.ToList());
        Assert.Equal(
            expected.Select(r => r.Up).ToArray(),
            ReferenceForum.Query(context, rounded.Select(r => r.Up)).Select(row => decimal.Parse(row[0]!, System.Globalization.CultureInfo.InvariantCulture)).ToArray());
    }

    [Fact]
    public void The_sign_of_a_number_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_sign_of_a_number_is_asked_for));
        using var context = database.Context();
        var source = ForumData.Users().OrderBy(u => u.Id).ToArray();

        // The whole number of the sign comes back as an integer, although H2 answers it in
        // the type of its argument.
        Assert.Equal(
            source.Select(u => Math.Sign(u.Reputation - 400)),
            context.Users.OrderBy(u => u.Id).Select(u => Math.Sign(u.Reputation - 400)).ToList());
        Assert.Equal(
            source.Select(u => Math.Sign(u.Balance)),
            context.Users.OrderBy(u => u.Id).Select(u => Math.Sign(u.Balance)).ToList());
    }

    [Fact]
    public void The_balance_is_combined_with_the_four_operations()
    {
        using var database = ForumDatabase.Filled(nameof(The_balance_is_combined_with_the_four_operations));
        using var context = database.Context();
        var source = ForumData.Users().OrderBy(u => u.Id).ToArray();

        var combined = context.Users.OrderBy(u => u.Id).Select(u => new
        {
            Added = u.Balance + 1.50m,
            Subtracted = u.Balance - 0.50m,
            Doubled = u.Balance * 2m,
            Quartered = u.Balance / 4m
        });
        var expected = source.Select(u => new
        {
            Added = u.Balance + 1.50m,
            Subtracted = u.Balance - 0.50m,
            Doubled = u.Balance * 2m,
            Quartered = u.Balance / 4m
        }).ToArray();

        Assert.Equal(expected, combined.ToList());
    }

    [Fact]
    public void The_absolute_balance_is_asked_for()
    {
        using var database = ForumDatabase.Filled(nameof(The_absolute_balance_is_asked_for));
        using var context = database.Context();
        var source = ForumData.Users().OrderBy(u => u.Id).ToArray();

        Assert.Equal(
            source.Select(u => Math.Abs(u.Balance)),
            context.Users.OrderBy(u => u.Id).Select(u => Math.Abs(u.Balance)).ToList());
        Assert.Equal(
            source.Select(u => Math.Abs(u.Balance - 5m)),
            context.Users.OrderBy(u => u.Id).Select(u => Math.Abs(u.Balance - 5m)).ToList());
    }
}
