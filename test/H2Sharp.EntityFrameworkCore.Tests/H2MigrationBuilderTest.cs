namespace H2Sharp.EntityFrameworkCore;

public class H2MigrationBuilderTest
{
    [Fact]
    public void IsH2_when_using_H2()
    {
        var migrationBuilder = new MigrationBuilder("H2Sharp.EntityFrameworkCore");
        Assert.True(migrationBuilder.IsH2());
    }

    [Fact]
    public void Not_IsH2_when_using_different_provider()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.InMemory");
        Assert.False(migrationBuilder.IsH2());
    }
}
