using H2Sharp.EntityFrameworkCore.Storage.Internal.Mapping;
using H2Sharp.EntityFrameworkCore.Storage.ValueConversion;
using H2Sharp.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable enable

namespace H2Sharp.EntityFrameworkCore.Storage;

/// <summary>
///     Collection mappings preserve the role values, order and nulls before a command is sent.
/// </summary>
public class H2CollectionConversionTests
{
    [Fact]
    public void Role_collections_convert_without_losing_nulls_or_order()
    {
        var elementConverter = new ValueConverter<UserRole, int>(role => (int)role, value => (UserRole)value);
        var arrays = new H2ArrayConverter<UserRole?[], UserRole?[], int?[]>(elementConverter);
        UserRole?[] values = [UserRole.Member, null, UserRole.Administrator, UserRole.Member];
        var stored = arrays.ConvertToProvider(values);
        Assert.Equal(new int?[] { (int)UserRole.Member, null, (int)UserRole.Administrator, (int)UserRole.Member }, stored);
        Assert.Equal(values, arrays.ConvertFromProvider(stored));
        Assert.Empty((int?[])arrays.ConvertToProvider(Array.Empty<UserRole?>())!);
        Assert.Null(arrays.ConvertToProvider(null));
        Assert.Null(arrays.ConvertFromProvider(null));

        var lists = new H2ArrayConverter<IList<UserRole>, List<UserRole>, int[]>(elementConverter);
        IList<UserRole> roles = new List<UserRole> { UserRole.Member, UserRole.Administrator };
        Assert.Equal(roles, (List<UserRole>)lists.ConvertFromProvider(lists.ConvertToProvider(roles))!);

        var sets = new H2ArrayConverter<HashSet<UserRole>, HashSet<UserRole>, int[]>(elementConverter);
        var unique = new HashSet<UserRole> { UserRole.Member, UserRole.Administrator };
        Assert.True(unique.SetEquals((HashSet<UserRole>)sets.ConvertFromProvider(sets.ConvertToProvider(unique))!));

        var streamed = new H2ArrayConverter<IEnumerable<UserRole>, List<UserRole>, int[]>(elementConverter);
        var sequence = roles.Where(_ => true);
        Assert.Equal(roles, (IEnumerable<UserRole>)streamed.ConvertFromProvider(streamed.ConvertToProvider(sequence))!);

        var listArrays = new H2ArrayConverter<List<int>, List<int>, int[]>();
        Assert.Equal(new[] { 1, 2, 1 }, listArrays.ConvertToProvider(new List<int> { 1, 2, 1 }));
        Assert.Equal(new[] { 1, 2, 1 }, (List<int>)listArrays.ConvertFromProvider(new[] { 1, 2, 1 })!);
    }

    [Fact]
    public void Collection_mapping_snapshots_are_independent_and_parameters_are_converted()
    {
        using var connection = new H2Connection("jdbc:h2:mem:collection_mappings");
        using var context = new DbContext(new DbContextOptionsBuilder().UseH2(connection).Options);
        var source = context.GetService<IRelationalTypeMappingSource>();
        var intMapping = source.FindMapping(typeof(int))!;
        var converter = new ValueConverter<UserRole, int>(role => (int)role, value => (UserRole)value);
        var roleMapping = (RelationalTypeMapping)intMapping.WithComposedConverter(converter);
        var mapping = new H2ArrayTypeMapping<List<UserRole>, List<UserRole>, UserRole>(roleMapping);
        var roles = new List<UserRole> { UserRole.Member, UserRole.Administrator };
        var snapshot = (List<UserRole>)mapping.Comparer.Snapshot(roles)!;
        Assert.True(mapping.Comparer.Equals(roles, snapshot));
        roles[0] = UserRole.Guest;
        Assert.False(mapping.Comparer.Equals(roles, snapshot));
        Assert.Equal(UserRole.Member, snapshot[0]);

        using var command = context.Database.GetDbConnection().CreateCommand();
        var parameter = mapping.CreateParameter(command, "roles", snapshot);
        Assert.Equal(new[] { (int)UserRole.Member, (int)UserRole.Administrator }, parameter.Value);
        var plain = new H2ArrayTypeMapping<int[], int[], int>(intMapping);
        var streamedParameter = plain.CreateParameter(command, "ids", Enumerable.Range(1, 3).Where(i => i > 1));
        Assert.Equal(new[] { 2, 3 }, (IEnumerable<int>)streamedParameter.Value!);
        Assert.Equal("ARRAY[2,3]", plain.GenerateSqlLiteral(new[] { 2, 3 }));
        var nullable = new H2ArrayTypeMapping<int?[], int?[], int?>(intMapping);
        Assert.Equal("ARRAY[2,NULL,3]", nullable.GenerateSqlLiteral(new int?[] { 2, null, 3 }));

        if (H2ReferenceOracle.IsAvailable)
        {
            var oracle = H2ReferenceOracle.Run($"SELECT 1 / CASE WHEN {nullable.GenerateSqlLiteral(new int?[] { 2, null, 3 })} IS NOT DISTINCT FROM ARRAY[2,NULL,3] THEN 1 ELSE 0 END;");
            Assert.True(oracle.Accepted, oracle.Output);
        }
    }

    private enum UserRole { Administrator, Member, Guest }
}
