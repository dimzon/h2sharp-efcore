/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MPL 2.0 or the EPL 1.0 (see LICENSE-H2.txt).
 *
 * H2 2.5 has no date/time infinities, so the CLR boundaries must be written as the real
 * minimum and maximum dates instead of PostgreSQL's -infinity/infinity.
 */
using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using Xunit;

namespace H2Sharp.EntityFrameworkCore.Storage;

public class H2TemporalInfinityLiteralTest
{
    [Fact]
    public void Date_boundaries_are_written_as_real_dates()
    {
        Assert.Equal("DATE '0001-01-01'", H2DateOnlyTypeMapping.Default.GenerateSqlLiteral(DateOnly.MinValue));
        Assert.Equal("DATE '9999-12-31'", H2DateOnlyTypeMapping.Default.GenerateSqlLiteral(DateOnly.MaxValue));
        Assert.Equal("DATE '0001-01-01'", H2DateTimeDateTypeMapping.Default.GenerateSqlLiteral(DateTime.MinValue));
        Assert.Equal("DATE '9999-12-31'", H2DateTimeDateTypeMapping.Default.GenerateSqlLiteral(DateTime.MaxValue));
    }

    [Fact]
    public void Timestamp_boundaries_are_written_as_real_timestamps()
    {
        Assert.Equal(
            "TIMESTAMP '0001-01-01 00:00:00'",
            H2TimestampTypeMapping.Default.GenerateSqlLiteral(DateTime.MinValue));
        Assert.Equal(
            "TIMESTAMP '9999-12-31 23:59:59.9999999'",
            H2TimestampTypeMapping.Default.GenerateSqlLiteral(DateTime.MaxValue));
    }

    [Fact]
    public void Timestamp_with_time_zone_boundaries_are_written_as_real_timestamps()
    {
        Assert.Equal(
            "TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00:00'",
            H2TimestampTzTypeMapping.Default.GenerateSqlLiteral(DateTimeOffset.MinValue));
        Assert.Equal(
            "TIMESTAMP WITH TIME ZONE '9999-12-31 23:59:59.9999999+00:00'",
            H2TimestampTzTypeMapping.Default.GenerateSqlLiteral(DateTimeOffset.MaxValue));
    }
}
