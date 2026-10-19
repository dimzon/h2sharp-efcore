using System.Data.Common;
using System.Globalization;
using System.Text;

namespace H2Sharp.EntityFrameworkCore.Infrastructure.Internal;

/// <summary>
///     Represents options managed by the H2 provider.
/// </summary>
/// <remarks>
///     <para>
///         Upstream this extension carried a data source builder, an admin database name, a server
///         version, a Redshift switch, TLS callbacks, password callbacks and user-defined ranges. H2
///         is an embedded engine with no server to dial, so there is nothing to authenticate to and no
///         version to negotiate: the store type is chosen by the caller and pinned at migration time.
///         What is left is enum mapping and null ordering, both of which H2 really has.
///     </para>
/// </remarks>
public class H2OptionsExtension : RelationalOptionsExtension
{
    private DbContextOptionsExtensionInfo? _info;
    private readonly List<EnumDefinition> _enumDefinitions;

    /// <summary>
    ///     The list of enum mappings specified by the user.
    /// </summary>
    public virtual IReadOnlyList<EnumDefinition> EnumDefinitions
        => _enumDefinitions;

    /// <summary>
    ///     True if nulls are ordered last; otherwise, false.
    /// </summary>
    /// <remarks>
    ///     H2 has no such default, so this is meaningful here in a way it was not upstream: H2's own
    ///     default puts nulls first for ascending order, while most applications expect them last.
    ///     The provider emits an explicit <c>NULLS LAST</c> rather than relying on a connection
    ///     setting, so the value recorded here drives generated SQL.
    /// </remarks>
    public virtual bool ReverseNullOrdering { get; private set; }

    /// <summary>
    ///     Initializes an instance of <see cref="H2OptionsExtension" /> with the default settings.
    /// </summary>
    public H2OptionsExtension()
        => _enumDefinitions = [];

    /// <summary>
    ///     Initializes an instance of <see cref="H2OptionsExtension" /> by copying the specified instance.
    /// </summary>
    /// <param name="copyFrom">The instance to copy.</param>
    public H2OptionsExtension(H2OptionsExtension copyFrom)
        : base(copyFrom)
    {
        _enumDefinitions = [..copyFrom._enumDefinitions];
        ReverseNullOrdering = copyFrom.ReverseNullOrdering;
    }

    /// <summary>
    ///     The default minimum batch size is 2. See https://github.com/aspnet/EntityFrameworkCore/pull/10091.
    /// </summary>
    public override int? MinBatchSize
        => base.MinBatchSize ?? 2;

    /// <inheritdoc />
    public override RelationalOptionsExtension WithConnectionString(string? connectionString)
        => (RelationalOptionsExtension)base.WithConnectionString(connectionString);

    /// <inheritdoc />
    public override RelationalOptionsExtension WithConnection(DbConnection? connection)
        => (RelationalOptionsExtension)base.WithConnection(connection);

    /// <summary>
    ///     Returns a copy of the current instance configured with the specified enum mapping.
    /// </summary>
    public virtual H2OptionsExtension WithEnumMapping(
        Type clrType,
        string? enumName,
        string? schemaName,
        IH2NameTranslator? nameTranslator)
    {
        if (clrType is not { IsEnum: true, IsClass: false })
        {
            throw new ArgumentException($"Enum type mappings require a CLR enum. {clrType.FullName} is not an enum.");
        }

        var clone = (H2OptionsExtension)Clone();

        nameTranslator ??= H2UpperCaseNameTranslator.Instance;

        clone._enumDefinitions.Add(new EnumDefinition(clrType, enumName, schemaName, nameTranslator));

        return clone;
    }

    /// <summary>
    ///     Returns a copy of the current instance configured to place nulls last in ascending order.
    /// </summary>
    public virtual H2OptionsExtension WithReverseNullOrdering(bool reverseNullOrdering)
    {
        var clone = (H2OptionsExtension)Clone();

        clone.ReverseNullOrdering = reverseNullOrdering;

        return clone;
    }

    #region Infrastructure

    /// <inheritdoc />
    protected override RelationalOptionsExtension Clone()
        => new H2OptionsExtension(this);

    /// <inheritdoc />
    public override void ApplyServices(IServiceCollection services)
        => services.AddEntityFrameworkH2();

    /// <inheritdoc />
    public override DbContextOptionsExtensionInfo Info
        => _info ??= new ExtensionInfo(this);

    private sealed class ExtensionInfo(IDbContextOptionsExtension extension) : RelationalExtensionInfo(extension)
    {
        private int? _serviceProviderHash;
        private string? _logFragment;

        private new H2OptionsExtension Extension
            => (H2OptionsExtension)base.Extension;

        public override bool IsDatabaseProvider
            => true;

        public override string LogFragment
        {
            get
            {
                if (_logFragment is not null)
                {
                    return _logFragment;
                }

                var builder = new StringBuilder(base.LogFragment);

                if (Extension.ReverseNullOrdering)
                {
                    builder.Append(nameof(Extension.ReverseNullOrdering)).Append(" ");
                }

                return _logFragment = builder.ToString();
            }
        }

        public override int GetServiceProviderHashCode()
        {
            if (_serviceProviderHash is null)
            {
                var hashCode = new HashCode();

                foreach (var enumDefinition in Extension._enumDefinitions)
                {
                    hashCode.Add(enumDefinition);
                }

                hashCode.Add(Extension.ReverseNullOrdering);

                _serviceProviderHash = hashCode.ToHashCode();
            }

            return _serviceProviderHash.Value;
        }

        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other)
            => other is ExtensionInfo otherInfo
                && Extension.ReverseNullOrdering == otherInfo.Extension.ReverseNullOrdering
                && Extension.EnumDefinitions.SequenceEqual(otherInfo.Extension.EnumDefinitions);

        /// <inheritdoc />
        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["H2Sharp.EntityFrameworkCore:" + nameof(H2DbContextOptionsBuilder.ReverseNullOrdering)]
                = Extension.ReverseNullOrdering.GetHashCode().ToString(CultureInfo.InvariantCulture);

            foreach (var enumDefinition in Extension._enumDefinitions)
            {
                debugInfo[
                        "H2Sharp.EntityFrameworkCore:"
                        + nameof(H2DbContextOptionsBuilder.MapEnum)
                        + ":"
                        + enumDefinition.ClrType.Name]
                    = enumDefinition.GetHashCode().ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    #endregion Infrastructure
}
