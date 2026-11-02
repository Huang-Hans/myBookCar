using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Booking.Domain.Reviews;

// 评价字段规则。评语先去除首尾空白，空白视为没有评语；长度按 Unicode 字符（码点）计，不按 UTF-16 单元。
public static class ReviewRules
{
    public static string? NormalizeComments(string? comments)
    {
        var trimmed = comments?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public static IReadOnlyList<FieldIssue> Validate(int rating, string? normalizedComments)
    {
        var issues = new List<FieldIssue>();
        if (rating is < TripReview.MinRating or > TripReview.MaxRating)
        {
            issues.Add(new FieldIssue("rating", $"Must be between {TripReview.MinRating} and {TripReview.MaxRating}"));
        }

        if (normalizedComments is not null && normalizedComments.EnumerateRunes().Count() > TripReview.MaxCommentLength)
        {
            issues.Add(new FieldIssue("comments", $"Must not exceed {TripReview.MaxCommentLength} characters"));
        }

        return issues;
    }
}
