using Microsoft.EntityFrameworkCore.TestModels.FunkyDataModel;
using H2Sharp.EntityFrameworkCore.TestUtilities;

namespace H2Sharp.EntityFrameworkCore.Query;

public class FunkyDataQueryH2Test : FunkyDataQueryTestBase<FunkyDataQueryH2Test.FunkyDataQueryH2Fixture>
{
    // ReSharper disable once UnusedParameter.Local
    public FunkyDataQueryH2Test(FunkyDataQueryH2Fixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        Fixture.TestSqlLoggerFactory.Clear();
        Fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    public override Task String_FirstOrDefault_and_LastOrDefault(bool async)
        => Task.CompletedTask; // H2 doesn't support reading an empty string as a char at the ADO level

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public async Task String_starts_with_on_argument_with_escape_constant(bool async)
        => await AssertQuery(
            async,
            ss => ss.Set<FunkyCustomer>().Where(c => c.FirstName.StartsWith("Some\\")),
            ss => ss.Set<FunkyCustomer>().Where(c => c.FirstName != null && c.FirstName.StartsWith("Some\\")));

    [ConditionalTheory]
    [MemberData(nameof(IsAsyncData))]
    public async Task String_starts_with_on_argument_with_escape_parameter(bool async)
    {
        var param = "Some\\";
        await AssertQuery(
            async,
            ss => ss.Set<FunkyCustomer>().Where(c => c.FirstName.StartsWith(param)),
            ss => ss.Set<FunkyCustomer>().Where(c => c.FirstName != null && c.FirstName.StartsWith(param)));
    }

    private void AssertSql(params string[] expected)
        => Fixture.TestSqlLoggerFactory.AssertBaseline(expected);

    public class FunkyDataQueryH2Fixture : FunkyDataQueryFixtureBase, ITestSqlLoggerFactory
    {
        private FunkyDataData _expectedData;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        protected override ITestStoreFactory TestStoreFactory
            => H2TestStoreFactory.Instance;

        public override FunkyDataContext CreateContext()
        {
            var context = base.CreateContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return context;
        }

        public override ISetSource GetExpectedData()
        {
            if (_expectedData is null)
            {
                _expectedData = (FunkyDataData)base.GetExpectedData();

                var maxId = _expectedData.FunkyCustomers.Max(c => c.Id);

                var mutableCustomersOhYeah = (List<FunkyCustomer>)_expectedData.FunkyCustomers;

                mutableCustomersOhYeah.Add(
                    new FunkyCustomer
                    {
                        Id = maxId + 1,
                        FirstName = "Some\\Guy",
                        LastName = null
                    });
            }

            return _expectedData;
        }

        protected override async Task SeedAsync(FunkyDataContext context)
        {
            context.FunkyCustomers.AddRange(GetExpectedData().Set<FunkyCustomer>());
            await context.SaveChangesAsync();
        }
    }
}
