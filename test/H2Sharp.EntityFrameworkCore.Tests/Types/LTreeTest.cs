namespace H2Sharp.EntityFrameworkCore.Types;

public class LTreeTest
{
    [ConditionalFact]
    public void ToString_works()
        => Assert.Equal("Top.Sub", ((LTree)"Top.Sub").ToString());
}
