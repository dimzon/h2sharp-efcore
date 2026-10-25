using System.Data;
using System.Text;

namespace H2Sharp.EntityFrameworkCore.Storage.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class H2SqlGenerationHelper : RelationalSqlGenerationHelper
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    public H2SqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
        : base(dependencies)
    {
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every identifier is quoted, always.
    ///     </para>
    ///     <para>
    ///         Upstream this asked the server for its reserved-word list and left a name unquoted when
    ///         the list allowed it. That is not available here, and more importantly it is not needed:
    ///         H2 folds an unquoted identifier to upper case, so a property named <c>title</c> would
    ///         reach the database as <c>TITLE</c> and the model would stop describing its own schema.
    ///         Leaving the quotes off is an optimisation, not a correctness win -- quoting costs two
    ///         characters and removes a whole class of silent mismatch.
    ///     </para>
    /// </remarks>
    public override string DelimitIdentifier(string identifier)
        => base.DelimitIdentifier(identifier);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    public override void DelimitIdentifier(StringBuilder builder, string identifier)
        => base.DelimitIdentifier(builder, identifier);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new EntityFramework Core release.
    /// </summary>
    /// <remarks>
    ///     The grammar of H2 2.5 requires the word <c>SAVEPOINT</c> here: <c>ROLLBACK [WORK] TO SAVEPOINT name</c>.
    ///     The default EF statement omits the word, the way PostgreSQL allows, and the engine rejects that.
    /// </remarks>
    public override string GenerateRollbackToSavepointStatement(string name)
        => "ROLLBACK TO SAVEPOINT " + DelimitIdentifier(name) + StatementTerminator;
}
