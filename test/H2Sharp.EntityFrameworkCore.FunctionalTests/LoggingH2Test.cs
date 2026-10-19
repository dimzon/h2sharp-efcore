using H2Sharp.EntityFrameworkCore.Diagnostics.Internal;
using H2Sharp.EntityFrameworkCore.Infrastructure;
using H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

namespace H2Sharp.EntityFrameworkCore;

public class LoggingH2Test : LoggingRelationalTestBase<H2DbContextOptionsBuilder, H2OptionsExtension>
{
    [Fact]
    public void Logs_context_initialization_admin_database()
        => Assert.Equal(
            ExpectedMessage($"AdminDatabase=foo {DefaultOptions}"),
            ActualMessage(s => CreateOptionsBuilder(s, b => ((H2DbContextOptionsBuilder)b).UseAdminDatabase("foo"))));

    [Fact]
    public void Logs_context_initialization_postgres_version()
        => Assert.Equal(
            ExpectedMessage($"PostgresVersion=10.7 {DefaultOptions}"),
            ActualMessage(s => CreateOptionsBuilder(s, b => ((H2DbContextOptionsBuilder)b).SetPostgresVersion(Version.Parse("10.7")))));

#pragma warning disable CS0618 // Authentication APIs on H2DbContextOptionsBuilder are obsolete
    [Fact]
    public void Logs_context_initialization_provide_client_certificates_callback()
        => Assert.Equal(
            ExpectedMessage($"ProvideClientCertificatesCallback {DefaultOptions}"),
            ActualMessage(
                s => CreateOptionsBuilder(
                    s, b => ((H2DbContextOptionsBuilder)b).ProvideClientCertificatesCallback(_ => { }))));

    [Fact]
    public void Logs_context_initialization_provide_password_callback()
        => Assert.Equal(
            ExpectedMessage($"ProvidePasswordCallback {DefaultOptions}"),
            ActualMessage(
                s => CreateOptionsBuilder(
                    s, b => ((H2DbContextOptionsBuilder)b).ProvidePasswordCallback((_, _, _, _) => "password"))));

    [Fact]
    public void Logs_context_initialization_remote_certificate_validation_callback()
        => Assert.Equal(
            ExpectedMessage($"RemoteCertificateValidationCallback {DefaultOptions}"),
            ActualMessage(
                s => CreateOptionsBuilder(
                    s,
                    b => ((H2DbContextOptionsBuilder)b).RemoteCertificateValidationCallback((_, _, _, _) => true))));
#pragma warning restore CS0618 // Authentication APIs on H2DbContextOptionsBuilder are obsolete

    [Fact]
    public void Logs_context_initialization_reverse_null_ordering()
        => Assert.Equal(
            ExpectedMessage($"ReverseNullOrdering {DefaultOptions}"),
            ActualMessage(s => CreateOptionsBuilder(s, b => ((H2DbContextOptionsBuilder)b).ReverseNullOrdering())));

    [Fact]
    public void Logs_context_initialization_user_range_definitions()
        => Assert.Equal(
            ExpectedMessage($"UserRangeDefinitions=[{typeof(int)}=>int4range] " + DefaultOptions),
            ActualMessage(s => CreateOptionsBuilder(s, b => ((H2DbContextOptionsBuilder)b).MapRange<int>("int4range"))));

    protected override DbContextOptionsBuilder CreateOptionsBuilder(
        IServiceCollection services,
        Action<RelationalDbContextOptionsBuilder<H2DbContextOptionsBuilder, H2OptionsExtension>> relationalAction)
        => new DbContextOptionsBuilder()
            .UseInternalServiceProvider(services.AddEntityFrameworkH2().BuildServiceProvider())
            .UseH2("Data Source=LoggingH2Test.db", relationalAction);

    protected override TestLogger CreateTestLogger()
        => new TestLogger<H2LoggingDefinitions>();

    protected override string ProviderName
        => "H2Sharp.EntityFrameworkCore";

    protected override string ProviderVersion
        => typeof(H2OptionsExtension).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
}
