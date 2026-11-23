namespace H2Sharp.EntityFrameworkCore;

// The F1 fixture seeds through SaveChanges, which the published engine now handles: the whole
// suite is back in the green run. The model fix is recorded in F1H2Fixture.cs and docs/BACKLOG.md.
public class SerializationH2Test(F1BytesH2Fixture fixture) : SerializationTestBase<F1BytesH2Fixture>(fixture);
