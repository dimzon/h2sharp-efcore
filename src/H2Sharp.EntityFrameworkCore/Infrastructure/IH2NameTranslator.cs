namespace H2Sharp.EntityFrameworkCore.Infrastructure;

/// <summary>
///     Translates CLR names into the database names H2 should store.
/// </summary>
/// <remarks>
///     <para>
///         This exists because the two databases disagree about what an unquoted identifier means. H2
///         folds an unquoted name to upper case; PostgreSQL folds it to lower case. So the provider
///         cannot assume either: it quotes everything it emits, and this translator decides what goes
///         between the quotes.
///     </para>
///     <para>
///         The default is upper case, which is what H2 produces on its own for an unquoted name. An
///         application that wants the PostgreSQL spelling calls
///         <c>ModelBuilder.UsePropertyAccessSyntax</c>-style overrides and supplies its own translator.
///     </para>
/// </remarks>
public interface IH2NameTranslator
{
    /// <summary>
    ///     Translates a table, column, index or sequence name.
    /// </summary>
    string Translate(string identifier);

    /// <summary>
    ///     Translates the name of a database type, e.g. for an enum type.
    /// </summary>
    string TranslateTypeName(string identifier);

    /// <summary>
    ///     Translates a CLR member name into the name of the column, row or value that backs it.
    /// </summary>
    string TranslateMemberName(string identifier);
}
