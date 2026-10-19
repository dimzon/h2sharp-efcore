using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>
///     Provides EF Core extension methods for H2 full-text search types.
/// </summary>
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public static class H2FullTextSearchLinqExtensions
{
    /// <summary>
    ///     AND tsquerys together. Generates the "&amp;&amp;" operator.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery And(this H2TsQuery query1, H2TsQuery query2)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(And)));

    /// <summary>
    ///     OR tsquerys together. Generates the "||" operator.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery Or(this H2TsQuery query1, H2TsQuery query2)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(Or)));

    /// <summary>
    ///     Negate a tsquery. Generates the "!!" operator.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery ToNegative(this H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(ToNegative)));

    /// <summary>
    ///     Returns whether <paramref name="query1" /> contains <paramref name="query2" />.
    ///     Generates the "@&gt;" operator.
    ///     http://www.postgresql.org/docs/current/static/functions-textsearch.html
    /// </summary>
    public static bool Contains(this H2TsQuery query1, H2TsQuery query2)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(Contains)));

    /// <summary>
    ///     Returns whether <paramref name="query1" /> is contained within <paramref name="query2" />.
    ///     Generates the "&lt;@" operator.
    ///     http://www.postgresql.org/docs/current/static/functions-textsearch.html
    /// </summary>
    public static bool IsContainedIn(this H2TsQuery query1, H2TsQuery query2)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(IsContainedIn)));

    /// <summary>
    ///     Returns the number of lexemes plus operators in <paramref name="query" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static int GetNodeCount(this H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(GetNodeCount)));

    /// <summary>
    ///     Get the indexable part of <paramref name="query" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static string GetQueryTree(this H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(GetQueryTree)));

    /// <summary>
    ///     Returns a string suitable for display containing a query match.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-HEADLINE
    /// </summary>
    public static string GetResultHeadline(this H2TsQuery query, string document)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(GetResultHeadline)));

    /// <summary>
    ///     Returns a string suitable for display containing a query match.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-HEADLINE
    /// </summary>
    public static string GetResultHeadline(this H2TsQuery query, string document, string options)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(GetResultHeadline)));

    /// <summary>
    ///     Returns a string suitable for display containing a query match using the text
    ///     search configuration specified by <paramref name="config" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-HEADLINE
    /// </summary>
    public static string GetResultHeadline(this H2TsQuery query, string config, string document, string options)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(GetResultHeadline)));

    /// <summary>
    ///     Searches <paramref name="query" /> for occurrences of <paramref name="target" />, and replaces
    ///     each occurrence with a <paramref name="substitute" />. All parameters are of type tsquery.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery Rewrite(this H2TsQuery query, H2TsQuery target, H2TsQuery substitute)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(Rewrite)));

    /// <summary>
    ///     For each row of the SQL <paramref name="select" /> result, occurrences of the first column value (the target)
    ///     are replaced by the second column value (the substitute) within the current <paramref name="query" /> value.
    ///     The <paramref name="select" /> must yield two columns of tsquery type.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery Rewrite(this H2TsQuery query, string select)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(Rewrite)));

    /// <summary>
    ///     Returns a tsquery that searches for a match to <paramref name="query1" /> followed by a match
    ///     to <paramref name="query2" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery ToPhrase(this H2TsQuery query1, H2TsQuery query2)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(ToPhrase)));

    /// <summary>
    ///     Returns a tsquery that searches for a match to <paramref name="query1" /> followed by a match
    ///     to <paramref name="query2" /> at a distance of <paramref name="distance" /> lexemes using
    ///     the &lt;N&gt; tsquery operator
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSQUERY
    /// </summary>
    public static H2TsQuery ToPhrase(this H2TsQuery query1, H2TsQuery query2, int distance)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsQuery) + "." + nameof(ToPhrase)));

    /// <summary>
    ///     This method generates the "@@" match operator. The <paramref name="query" /> parameter is
    ///     assumed to be a plain search query and will be converted to a tsquery using plainto_tsquery.
    ///     http://www.postgresql.org/docs/current/static/textsearch-intro.html#TEXTSEARCH-MATCHING
    /// </summary>
    public static bool Matches(this H2TsVector vector, string query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Matches)));

    /// <summary>
    ///     This method generates the "@@" match operator.
    ///     http://www.postgresql.org/docs/current/static/textsearch-intro.html#TEXTSEARCH-MATCHING
    /// </summary>
    public static bool Matches(this H2TsVector vector, H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Matches)));

    /// <summary>
    ///     Returns a vector which combines the lexemes and positional information of <paramref name="vector1" />
    ///     and <paramref name="vector2" /> using the || tsvector operator. Positions and weight labels are retained
    ///     during the concatenation.
    ///     https://www.postgresql.org/docs/10/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static H2TsVector Concat(this H2TsVector vector1, H2TsVector vector2)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Concat)));

    /// <summary>
    ///     Assign weight to each element of <paramref name="vector" /> and return a new
    ///     weighted tsvector.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static H2TsVector SetWeight(this H2TsVector vector, H2TsVector.Lexeme.Weight weight)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(SetWeight)));

    /// <summary>
    ///     Assign weight to elements of <paramref name="vector" /> that are in <paramref name="lexemes" /> and
    ///     return a new weighted tsvector.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static H2TsVector SetWeight(this H2TsVector vector, H2TsVector.Lexeme.Weight weight, string[] lexemes)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(SetWeight)));

    /// <summary>
    ///     Assign weight to each element of <paramref name="vector" /> and return a new
    ///     weighted tsvector.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static H2TsVector SetWeight(this H2TsVector vector, char weight)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(SetWeight)));

    /// <summary>
    ///     Assign weight to elements of <paramref name="vector" /> that are in <paramref name="lexemes" /> and
    ///     return a new weighted tsvector.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static H2TsVector SetWeight(this H2TsVector vector, char weight, string[] lexemes)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(SetWeight)));

    /// <summary>
    ///     Return a new vector with <paramref name="lexeme" /> removed from <paramref name="vector" />
    ///     https://www.postgresql.org/docs/current/static/functions-textsearch.html
    /// </summary>
    public static H2TsVector Delete(this H2TsVector vector, string lexeme)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Delete)));

    /// <summary>
    ///     Return a new vector with <paramref name="lexemes" /> removed from <paramref name="vector" />
    ///     https://www.postgresql.org/docs/current/static/functions-textsearch.html
    /// </summary>
    public static H2TsVector Delete(this H2TsVector vector, string[] lexemes)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Delete)));

    /// <summary>
    ///     Returns a new vector with only lexemes having weights specified in <paramref name="weights" />.
    ///     https://www.postgresql.org/docs/current/static/functions-textsearch.html
    /// </summary>
    public static H2TsVector Filter(this H2TsVector vector, char[] weights)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Filter)));

    /// <summary>
    ///     Returns the number of lexemes in <paramref name="vector" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static int GetLength(this H2TsVector vector)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(GetLength)));

    /// <summary>
    ///     Removes weights and positions from <paramref name="vector" /> and returns
    ///     a new stripped tsvector.
    ///     http://www.postgresql.org/docs/current/static/textsearch-features.html#TEXTSEARCH-MANIPULATE-TSVECTOR
    /// </summary>
    public static H2TsVector ToStripped(this H2TsVector vector)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(ToStripped)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this H2TsVector vector, H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Rank)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> while normalizing
    ///     the result according to the behaviors specified by <paramref name="normalization" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this H2TsVector vector, H2TsQuery query, H2TsRankingNormalization normalization)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Rank)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> with custom
    ///     weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(this H2TsVector vector, float[] weights, H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Rank)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> while normalizing
    ///     the result according to the behaviors specified by <paramref name="normalization" />
    ///     and using custom weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float Rank(
        this H2TsVector vector,
        float[] weights,
        H2TsQuery query,
        H2TsRankingNormalization normalization)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(Rank)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover
    ///     density method.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this H2TsVector vector, H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(RankCoverDensity)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover
    ///     density method while normalizing the result according to the behaviors specified by
    ///     <paramref name="normalization" />.
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this H2TsVector vector, H2TsQuery query, H2TsRankingNormalization normalization)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(RankCoverDensity)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover
    ///     density method with custom weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(this H2TsVector vector, float[] weights, H2TsQuery query)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(RankCoverDensity)));

    /// <summary>
    ///     Calculates the rank of <paramref name="vector" /> for <paramref name="query" /> using the cover density
    ///     method while normalizing the result according to the behaviors specified by <paramref name="normalization" />
    ///     and using custom weighting for word instances depending on their labels (D, C, B or A).
    ///     http://www.postgresql.org/docs/current/static/textsearch-controls.html#TEXTSEARCH-RANKING
    /// </summary>
    public static float RankCoverDensity(
        this H2TsVector vector,
        float[] weights,
        H2TsQuery query,
        H2TsRankingNormalization normalization)
        => throw new InvalidOperationException(CoreStrings.FunctionOnClient(nameof(H2TsVector) + "." + nameof(RankCoverDensity)));
}
