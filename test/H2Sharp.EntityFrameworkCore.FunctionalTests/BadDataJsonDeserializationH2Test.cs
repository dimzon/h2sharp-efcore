#nullable enable

namespace H2Sharp.EntityFrameworkCore;

public class BadDataJsonDeserializationH2Test : BadDataJsonDeserializationTestBase
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => base.OnConfiguring(optionsBuilder.UseH2(b => b.UseNetTopologySuite()));
}
