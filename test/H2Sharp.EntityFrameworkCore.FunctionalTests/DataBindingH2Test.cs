namespace H2Sharp.EntityFrameworkCore;

// The suite runs green against the published engine: batch saves and the shared test-store cache
// keep the F1 fixture's seeded database intact. The model fix is in F1H2Fixture.cs and docs/BACKLOG.md.
public class DataBindingH2Test(F1BytesH2Fixture fixture) : DataBindingTestBase<F1BytesH2Fixture>(fixture);
