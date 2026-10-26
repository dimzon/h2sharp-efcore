namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the F1 fixture seeds its tables on first use, and seeding goes through
// SaveChanges, which the port stops with "Not ported yet: Prepared.update". The model itself builds
// and the tables are created -- the earlier failure, "The entity type 'Driver' requires a primary
// key", was ours: see F1H2Fixture.cs and docs/BACKLOG.md, item 4.
[Trait("Category", "BlockedByPort")]
public class DataBindingH2Test(F1BytesH2Fixture fixture) : DataBindingTestBase<F1BytesH2Fixture>(fixture);
