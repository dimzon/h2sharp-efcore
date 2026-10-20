using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

public static class TestEnvironment
{
    public static IConfiguration Config { get; }

    static TestEnvironment()
    {
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: true)
            .AddJsonFile("config.test.json", optional: true)
            .AddEnvironmentVariables();

        Config = configBuilder.Build()
            .GetSection("Test:H2");

        Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
    }

    private const string DefaultConnectionString = "jdbc:h2:mem:efcore_tests";

    public static string DefaultConnection
        => Config["DefaultConnection"] ?? DefaultConnectionString;

    private static Version _postgresVersion;

    public static Version PostgresVersion
    {
        get
        {
            if (_postgresVersion is not null)
            {
                return _postgresVersion;
            }

            using var conn = new H2Connection(H2TestStore.CreateConnectionString("postgres"));
            conn.Open();
            return _postgresVersion = Version.Parse(conn.ServerVersion);
        }
    }

    /// <summary>
    ///     Whether the database has PostGIS.
    /// </summary>
    /// <remarks>
    ///     H2 has no PostGIS, and the answer does not depend on the database: it is "no". The
    ///     question was asked of the database before ("SELECT EXISTS (SELECT 1 FROM
    ///     pg_available_extensions …)"), and that question killed the test host: the engine does
    ///     not read EXISTS, the exception came out of the check of the condition of a test, and
    ///     an exception there is not caught by anybody -- xunit rethrows it on the thread pool,
    ///     and the process dies with it. The rest of the run never happens.
    /// </remarks>
    public static bool IsPostgisAvailable => false;
}
