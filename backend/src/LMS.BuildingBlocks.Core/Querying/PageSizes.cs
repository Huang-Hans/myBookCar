namespace LMS.BuildingBlocks.Core.Querying;

// 合同规定的分页参数：页大小只允许 10 / 20 / 50，默认 10，页号从 1 开始。
public static class PageSizes
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 10;

    public static IReadOnlyList<int> Allowed { get; } = [10, 20, 50];

    public static bool IsAllowed(int pageSize) => pageSize is 10 or 20 or 50;
}
