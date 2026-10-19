namespace H2Sharp.EntityFrameworkCore;

public class ModelBuilding101H2Test : ModelBuilding101RelationalTestBase
{
    protected override DbContextOptionsBuilder ConfigureContext(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseH2();
}
