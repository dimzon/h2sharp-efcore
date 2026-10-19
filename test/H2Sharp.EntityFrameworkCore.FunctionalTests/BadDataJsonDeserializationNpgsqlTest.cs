#nullable enable

namespace H2Sharp.EntityFrameworkCore;

public class BadDataJsonDeserializationNpgsqlTest : BadDataJsonDeserializationTestBase
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => base.OnConfiguring(optionsBuilder.UseH2(b => b.UseNetTopologySuite()));
}
