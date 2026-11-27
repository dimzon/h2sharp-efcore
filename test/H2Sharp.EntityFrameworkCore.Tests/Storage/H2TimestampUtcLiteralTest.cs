/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MPL 2.0 or the EPL 1.0 (see LICENSE-H2.txt).
 *
 * H2's TIMESTAMP WITHOUT TIME ZONE has no offset: a UTC DateTime's components are written
 * as wall clock time instead of being rejected the way the PG provider rejects them.
 */
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Storage;

public class H2TimestampUtcLiteralTest
{
    [Fact]
    public void Utc_DateTime_is_written_as_a_timestamp_literal()
    {
        var value = new DateTime(2024, 3, 5, 12, 30, 45, DateTimeKind.Utc);

        Assert.Equal(
            "TIMESTAMP '2024-03-05 12:30:45'",
            H2TimestampTypeMapping.Default.GenerateSqlLiteral(value));
    }
}
