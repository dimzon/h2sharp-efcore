namespace H2Sharp.EntityFrameworkCore.Infrastructure;

/// <summary>
///     The default name translator: folds names to upper case.
/// </summary>
/// <remarks>
///     Upper case is H2's own folding rule for an unquoted identifier, so a name translated this way
///     is reachable without quoting as well as with it. That is the safest default here: it is the
///     spelling a plain H2 client would have written by hand.
/// </remarks>
public sealed class H2UpperCaseNameTranslator : IH2NameTranslator
{
    /// <summary>
    ///     The shared instance. The translator is stateless.
    /// </summary>
    public static readonly H2UpperCaseNameTranslator Instance = new();

    /// <inheritdoc />
    public string Translate(string identifier)
        => Check.NotNull(identifier, nameof(identifier)).ToUpperInvariant();

    /// <inheritdoc />
    public string TranslateTypeName(string identifier)
        => Check.NotNull(identifier, nameof(identifier)).ToUpperInvariant();

    /// <inheritdoc />
    public string TranslateMemberName(string identifier)
        => Check.NotNull(identifier, nameof(identifier)).ToUpperInvariant();
}
