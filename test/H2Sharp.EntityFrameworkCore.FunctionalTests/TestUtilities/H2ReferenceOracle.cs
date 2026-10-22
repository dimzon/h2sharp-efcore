#nullable enable
using System.Diagnostics;
using System.Text;

namespace H2Sharp.EntityFrameworkCore.TestUtilities;

/// <summary>
///     Runs SQL against the reference H2 engine and reports what it says.
/// </summary>
/// <remarks>
///     <para>
///         The provider has to work against H2 2.5, not against whatever the port happens to
///         implement today. The port is being filled in separately, so a test that runs against the
///         port proves only that the port agrees with itself.
///     </para>
///     <para>
///         This runs the reference engine instead: the pinned jar from the h2sharp repository, on the
///         JVM. A script the reference accepts is a script H2 2.5 accepts, and that is the only
///         statement worth making about the provider while the port catches up.
///     </para>
///     <para>
///         The reference jar is not in this repository. The path comes from
///         <c>H2SHARP_REFERENCE_H2_JAR</c>, or from the sibling h2sharp checkout, and when neither
///         is there the test skips rather than fails: a missing reference is not a provider defect.
///     </para>
/// </remarks>
public static class H2ReferenceOracle
{
    private const string DefaultJarPath =
        @"C:\Work\h2sharp\reference\h2\h2\target\h2-2.5.259-SNAPSHOT.jar";

    /// <summary>
    ///     The reference jar, or <see langword="null" /> when it cannot be found.
    /// </summary>
    public static string? JarPath
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("H2SHARP_REFERENCE_H2_JAR");
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                return configured;
            }

            return File.Exists(DefaultJarPath) ? DefaultJarPath : null;
        }
    }

    /// <summary>
    ///     Whether the reference engine can be run.
    /// </summary>
    public static bool IsAvailable => JarPath is not null;

    /// <summary>
    ///     Runs a script against a fresh in-memory reference database.
    /// </summary>
    /// <param name="script">The SQL to run.</param>
    /// <returns>What the reference engine said, and whether it accepted the script.</returns>
    public static (bool Accepted, string Output) Run(string script)
    {
        var jar = JarPath ?? throw new InvalidOperationException(
            "The reference H2 jar is not available. Set H2SHARP_REFERENCE_H2_JAR to its path.");

        var work = Path.Combine(Path.GetTempPath(), "h2sharp-ef-oracle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var scriptPath = Path.Combine(work, "script.sql");
            File.WriteAllText(scriptPath, script, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            // Java 11 and later can run a single source file directly, so no separate compile step
            // is needed and the reference is used exactly as it ships.
            var runnerPath = Path.Combine(work, "RunScript.java");
            File.WriteAllText(runnerPath, RunnerSource, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var startInfo = new ProcessStartInfo("java")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-cp");
            startInfo.ArgumentList.Add(jar);
            startInfo.ArgumentList.Add(runnerPath);
            startInfo.ArgumentList.Add(scriptPath);

            using var process = Process.Start(startInfo)!;
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            var output = string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;

            return (process.ExitCode == 0, output.Trim());
        }
        finally
        {
            try
            {
                Directory.Delete(work, recursive: true);
            }
            catch (IOException)
            {
                // A leftover temporary directory is not worth failing a test over.
            }
        }
    }

    private const string RunnerSource = """
        import java.io.BufferedReader;
        import java.io.FileReader;
        import java.sql.Connection;
        import java.sql.DriverManager;
        import java.sql.Statement;
        import org.h2.util.ScriptReader;

        public class RunScript {
            public static void main(String[] args) throws Exception {
                Class.forName("org.h2.Driver");
                try (Connection c = DriverManager.getConnection("jdbc:h2:mem:oracle")) {
                    try (ScriptReader reader = new ScriptReader(new BufferedReader(new FileReader(args[0])))) {
                        String statement;
                        while ((statement = reader.readStatement()) != null) {
                            if (statement.isBlank()) {
                                continue;
                            }
                            try (Statement s = c.createStatement()) {
                                s.execute(statement);
                            }
                        }
                    }
                    System.out.println("OK");
                }
            }
        }
        """;
}
