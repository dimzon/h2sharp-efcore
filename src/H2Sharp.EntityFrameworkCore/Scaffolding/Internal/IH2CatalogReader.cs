/*
 * Copyright 2026 H2Sharp contributors.
 * Licensed under the MPL 2.0 or the EPL 1.0 (see LICENSE-H2.txt).
 *
 * The typeless INFORMATION_SCHEMA rows are read once, here, and handed to the
 * scaffolder and the database creator as typed catalog records. The SQL stays in
 * one place; the callers only decide what the metadata means for EF Core.
 */
using System.Data.Common;

// Public only so EF's DI can pass the reader to the public provider constructors; the catalog
// records are an internal contract and are not part of the supported public API.
#pragma warning disable CS1591

namespace H2Sharp.EntityFrameworkCore.Scaffolding.Internal;

/// <summary>
///     Reads H2's INFORMATION_SCHEMA catalogue into typed rows. Implementations must keep the
///     caller's connection state untouched: scaffolding can be given an already-open connection
///     to an in-memory database.
/// </summary>
public interface IH2CatalogReader
{
    bool IsAvailable(DbConnection connection);

    IReadOnlyList<string> GetSchemas(DbConnection connection);

    IReadOnlyList<H2TableRow> GetTables(DbConnection connection, string schema);

    IReadOnlyList<H2ColumnRow> GetColumns(DbConnection connection, string schema);

    IReadOnlyList<H2KeyRow> GetKeys(DbConnection connection, string schema);

    IReadOnlyList<H2IndexRow> GetIndexes(DbConnection connection, string schema);

    IReadOnlyList<H2SequenceRow> GetSequences(DbConnection connection, string schema);

    IReadOnlyList<H2EnumValueRow> GetEnums(DbConnection connection, string schema);

    IReadOnlyList<H2ForeignKeyRow> GetForeignKeys(DbConnection connection);

    string? GetPrincipalTableName(DbConnection connection, string? schema, string constraintName);

    IReadOnlyList<string> GetPrincipalColumns(DbConnection connection, string? schema, string constraintName);

    bool HasTables(DbConnection connection, string schema);

    bool SchemaExists(DbConnection connection, string schema);
}

public sealed record H2TableRow(string Name, string? Type, string? Comment);

public sealed record H2ColumnRow(
    string TableName,
    string ColumnName,
    string? DataType,
    string IsNullable,
    string IsIdentity,
    string? IdentityGeneration,
    string IsGenerated,
    string? GenerationExpression,
    string? ColumnDefault,
    string? DeclaredDataType,
    string? Remarks,
    long? CharacterMaximumLength,
    int? NumericPrecision,
    int? NumericScale,
    int? DateTimePrecision,
    int? DeclaredNumericPrecision,
    int? DeclaredNumericScale,
    long? IdentityStart,
    long? IdentityIncrement,
    long? IdentityMinimum,
    long? IdentityMaximum,
    string? IdentityCycle,
    long? IdentityCache);

public sealed record H2KeyRow(
    string? TableName,
    string? ConstraintName,
    string? ConstraintType,
    string? ColumnName);

public sealed record H2IndexRow(
    string? IndexName,
    string? TableName,
    string? IndexTypeName,
    string? ColumnName,
    string? OrderingSpecification,
    string? NullOrdering,
    bool? IsUnique);

public sealed record H2SequenceRow(
    string Name,
    string? DataType,
    long? StartValue,
    int? Increment,
    long? MinimumValue,
    long? MaximumValue,
    bool IsCyclic);

public sealed record H2EnumValueRow(string Name, string Value);

public sealed record H2ForeignKeyRow(
    string? TableSchema,
    string? TableName,
    string? ConstraintName,
    string? ColumnName,
    string? UniqueConstraintSchema,
    string? UniqueConstraintName,
    string? UpdateRule,
    string? DeleteRule);

#pragma warning restore CS1591
