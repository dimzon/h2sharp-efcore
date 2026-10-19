using System.Net;
using System.Net.NetworkInformation;
using H2Sharp.EntityFrameworkCore.Query.Expressions;
using static H2Sharp.EntityFrameworkCore.Utilities.Statics;

namespace H2Sharp.EntityFrameworkCore.Query.ExpressionTranslators.Internal;

/// <summary>
///     Provides translation services for operators and functions of PostgreSQL network typess (cidr, inet, macaddr, macaddr8).
/// </summary>
/// <remarks>
///     See: https://www.postgresql.org/docs/current/static/functions-net.html
/// </remarks>
public class H2NetworkTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo IPAddressParse =
        typeof(IPAddress).GetRuntimeMethod(nameof(IPAddress.Parse), [typeof(string)])!;

    private static readonly MethodInfo PhysicalAddressParse =
        typeof(PhysicalAddress).GetRuntimeMethod(nameof(PhysicalAddress.Parse), [typeof(string)])!;

    private readonly H2SqlExpressionFactory _sqlExpressionFactory;

    private readonly RelationalTypeMapping _inetMapping;
    private readonly RelationalTypeMapping _cidrMapping;
    private readonly RelationalTypeMapping _macaddr8Mapping;
    private readonly RelationalTypeMapping _longAddressMapping;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public H2NetworkTranslator(
        IRelationalTypeMappingSource typeMappingSource,
        H2SqlExpressionFactory sqlExpressionFactory,
        IModel model)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
        _inetMapping = typeMappingSource.FindMapping("inet")!;
        _cidrMapping = typeMappingSource.FindMapping("cidr")!;
        _macaddr8Mapping = typeMappingSource.FindMapping("macaddr8")!;
        _longAddressMapping = typeMappingSource.FindMapping(typeof(long), model)!;
    }

    /// <inheritdoc />
    public virtual SqlExpression? Translate(
        SqlExpression? instance,
        MethodInfo method,
        IReadOnlyList<SqlExpression> arguments,
        IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method == IPAddressParse)
        {
            return _sqlExpressionFactory.Convert(arguments[0], typeof(IPAddress));
        }

        if (method == PhysicalAddressParse)
        {
            return _sqlExpressionFactory.Convert(arguments[0], typeof(PhysicalAddress));
        }

        if (method.DeclaringType == typeof(H2NetworkDbFunctionsExtensions))
        {
            var paramType = method.GetParameters()[1].ParameterType;

            if (paramType == typeof(H2Inet))
            {
                return TranslateInetExtensionMethod(method, arguments);
            }

            if (paramType == typeof(H2Cidr))
            {
                return TranslateCidrExtensionMethod(method, arguments);
            }

            if (paramType == typeof(PhysicalAddress))
            {
                return TranslateMacaddrExtensionMethod(method, arguments);
            }
        }

        return null;
    }

    private SqlExpression? TranslateInetExtensionMethod(MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        => method.Name switch
        {
            nameof(H2NetworkDbFunctionsExtensions.LessThan)
                => new SqlBinaryExpression(
                    ExpressionType.LessThan,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.LessThanOrEqual)
                => new SqlBinaryExpression(
                    ExpressionType.LessThanOrEqual,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.GreaterThanOrEqual)
                => new SqlBinaryExpression(
                    ExpressionType.GreaterThanOrEqual,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.GreaterThan)
                => new SqlBinaryExpression(
                    ExpressionType.GreaterThan,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.ContainedBy)
                => _sqlExpressionFactory.ContainedBy(arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.ContainedByOrEqual)
                => _sqlExpressionFactory.MakePostgresBinary(
                    PgExpressionType.NetworkContainedByOrEqual, arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.Contains)
                => _sqlExpressionFactory.Contains(arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.ContainsOrEqual)
                => _sqlExpressionFactory.MakePostgresBinary(PgExpressionType.NetworkContainsOrEqual, arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.ContainsOrContainedBy)
                => _sqlExpressionFactory.MakePostgresBinary(
                    PgExpressionType.NetworkContainsOrContainedBy, arguments[1], arguments[2]),

            nameof(H2NetworkDbFunctionsExtensions.BitwiseNot)
                => new SqlUnaryExpression(
                    ExpressionType.Not,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.BitwiseAnd)
                => new SqlBinaryExpression(
                    ExpressionType.And,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.BitwiseOr)
                => new SqlBinaryExpression(
                    ExpressionType.Or,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.Add)
                => new SqlBinaryExpression(
                    ExpressionType.Add,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.Subtract) when arguments[2].Type == typeof(long)
                => new SqlBinaryExpression(
                    ExpressionType.Subtract,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(H2Inet),
                    _inetMapping),

            nameof(H2NetworkDbFunctionsExtensions.Subtract)
                => new SqlBinaryExpression(
                    ExpressionType.Subtract,
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[1]),
                    _sqlExpressionFactory.ApplyDefaultTypeMapping(arguments[2]),
                    typeof(long),
                    _longAddressMapping),

            nameof(H2NetworkDbFunctionsExtensions.Abbreviate)
                => NullPropagatingFunction("abbrev", [arguments[1]], typeof(string)),
            nameof(H2NetworkDbFunctionsExtensions.Broadcast)
                => NullPropagatingFunction("broadcast", [arguments[1]], typeof(IPAddress), _inetMapping),
            nameof(H2NetworkDbFunctionsExtensions.Family)
                => NullPropagatingFunction("family", [arguments[1]], typeof(int)),
            nameof(H2NetworkDbFunctionsExtensions.Host)
                => NullPropagatingFunction("host", [arguments[1]], typeof(string)),
            nameof(H2NetworkDbFunctionsExtensions.HostMask)
                => NullPropagatingFunction("hostmask", [arguments[1]], typeof(IPAddress), _inetMapping),
            nameof(H2NetworkDbFunctionsExtensions.MaskLength)
                => NullPropagatingFunction("masklen", [arguments[1]], typeof(int)),
            nameof(H2NetworkDbFunctionsExtensions.Netmask)
                => NullPropagatingFunction("netmask", [arguments[1]], typeof(IPAddress), _inetMapping),
            nameof(H2NetworkDbFunctionsExtensions.Network)
                => NullPropagatingFunction("network", [arguments[1]], typeof((IPAddress Address, int Subnet)), _cidrMapping),
            nameof(H2NetworkDbFunctionsExtensions.SetMaskLength)
                => NullPropagatingFunction(
                    "set_masklen", [arguments[1], arguments[2]], arguments[1].Type, arguments[1].TypeMapping),
            nameof(H2NetworkDbFunctionsExtensions.Text)
                => NullPropagatingFunction("text", [arguments[1]], typeof(string)),
            nameof(H2NetworkDbFunctionsExtensions.SameFamily)
                => NullPropagatingFunction("inet_same_family", [arguments[1], arguments[2]], typeof(bool)),
            nameof(H2NetworkDbFunctionsExtensions.Merge)
                => NullPropagatingFunction(
                    "inet_merge", [arguments[1], arguments[2]], typeof((IPAddress Address, int Subnet)), _cidrMapping),

            _ => null
        };

    private SqlExpression? TranslateCidrExtensionMethod(MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        => method.Name switch
        {
            nameof(H2NetworkDbFunctionsExtensions.Abbreviate)
                => NullPropagatingFunction("abbrev", [arguments[1]], typeof(string)),
            nameof(H2NetworkDbFunctionsExtensions.SetMaskLength)
                => NullPropagatingFunction(
                    "set_masklen", [arguments[1], arguments[2]], arguments[1].Type, arguments[1].TypeMapping),

            _ => null
        };

    private SqlExpression? TranslateMacaddrExtensionMethod(MethodInfo method, IReadOnlyList<SqlExpression> arguments)
        => method.Name switch
        {
            nameof(H2NetworkDbFunctionsExtensions.LessThan)
                => _sqlExpressionFactory.LessThan(arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.LessThanOrEqual)
                => _sqlExpressionFactory.LessThanOrEqual(arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.GreaterThanOrEqual)
                => _sqlExpressionFactory.GreaterThanOrEqual(arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.GreaterThan)
                => _sqlExpressionFactory.GreaterThan(arguments[1], arguments[2]),

            nameof(H2NetworkDbFunctionsExtensions.BitwiseNot)
                => _sqlExpressionFactory.Not(arguments[1]),
            nameof(H2NetworkDbFunctionsExtensions.BitwiseAnd)
                => _sqlExpressionFactory.And(arguments[1], arguments[2]),
            nameof(H2NetworkDbFunctionsExtensions.BitwiseOr)
                => _sqlExpressionFactory.Or(arguments[1], arguments[2]),

            nameof(H2NetworkDbFunctionsExtensions.Truncate) => NullPropagatingFunction(
                "trunc", [arguments[1]], typeof(PhysicalAddress), arguments[1].TypeMapping),
            nameof(H2NetworkDbFunctionsExtensions.Set7BitMac8) => NullPropagatingFunction(
                "macaddr8_set7bit", [arguments[1]], typeof(PhysicalAddress), _macaddr8Mapping),

            _ => null
        };

    private SqlExpression NullPropagatingFunction(
        string name,
        SqlExpression[] arguments,
        Type returnType,
        RelationalTypeMapping? typeMapping = null)
        => _sqlExpressionFactory.Function(
            name,
            arguments,
            nullable: true,
            argumentsPropagateNullability: TrueArrays[arguments.Length],
            returnType,
            typeMapping);
}
