/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MPL 2.0 or the EPL 1.0 (see LICENSE-H2.txt).
 *
 * H2 2.5 has no PostgreSQL LOCK TABLE. The migration lock is a MERGE on the primary key
 * of a one-row table; these statements are the ones H2HistoryRepository issues.
 */
using H2Sharp.Data;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Migrations;

public class H2MigrationLockSqlTests
{
    [Fact]
    public void TheMigrationLockStatementsAreValidH2()
    {
        using var connection = new H2Connection(
            "Data Source=migration_lock_" + Guid.NewGuid().ToString("N") + ";Storage=Memory");
        connection.Open();

        Execute(connection, "CREATE TABLE IF NOT EXISTS \"__H2SharpMigrationLock\" (\"Id\" INTEGER PRIMARY KEY)");
        Execute(connection, "MERGE INTO \"__H2SharpMigrationLock\" (\"Id\") KEY (\"Id\") VALUES (1)");
        Execute(connection, "CREATE TABLE IF NOT EXISTS \"__H2SharpMigrationLock\" (\"Id\" INTEGER PRIMARY KEY)");
        Execute(connection, "MERGE INTO \"__H2SharpMigrationLock\" (\"Id\") KEY (\"Id\") VALUES (1)");

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__H2SharpMigrationLock\"";
        Assert.Equal(1L, System.Convert.ToInt64(command.ExecuteScalar()));
    }

    private static void Execute(H2Connection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
