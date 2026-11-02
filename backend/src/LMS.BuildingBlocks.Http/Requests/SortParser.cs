using LMS.BuildingBlocks.Core.Querying;

namespace LMS.BuildingBlocks.Http.Requests;

internal static class SortParser
{
    private const char DescendingPrefix = '-';

    public static IReadOnlyList<SortTerm> Parse(string? raw, ListQuerySpec spec, FieldErrorCollector errors)
    {
        var text = string.IsNullOrWhiteSpace(raw) ? spec.DefaultSort : raw;
        var terms = text.Split(',').Select(token => ParseToken(token.Trim(), spec, errors)).OfType<SortTerm>().ToList();

        return WithStableField(DistinctByField(terms), spec);
    }

    private static SortTerm? ParseToken(string token, ListQuerySpec spec, FieldErrorCollector errors)
    {
        var isDescending = token.StartsWith(DescendingPrefix);
        var field = isDescending ? token[1..] : token;
        if (spec.SortFields.Contains(field, StringComparer.Ordinal))
        {
            return new SortTerm(field, isDescending);
        }

        errors.Add("sort", $"Unsupported sort field '{field}'");
        return null;
    }

    private static List<SortTerm> DistinctByField(List<SortTerm> terms) =>
        terms.GroupBy(term => term.Field, StringComparer.Ordinal).Select(group => group.First()).ToList();

    private static IReadOnlyList<SortTerm> WithStableField(List<SortTerm> terms, ListQuerySpec spec)
    {
        if (!terms.Exists(term => term.Field == spec.StableField))
        {
            terms.Add(new SortTerm(spec.StableField, false));
        }

        return terms;
    }
}
