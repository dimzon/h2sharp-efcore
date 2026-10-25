namespace H2Sharp.EntityFrameworkCore;

// BlockedByPort: the fixture's model carries UNIQUE table constraints, and the port's
// CREATE TABLE reads only the primary key of them ("expected PRIMARY"). The reference accepts
// the statement the provider writes (checked on the jar).
[Trait("Category", "BlockedByPort")]
public class DataBindingH2Test(F1BytesH2Fixture fixture) : DataBindingTestBase<F1BytesH2Fixture>(fixture);
