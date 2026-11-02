namespace LMS.Platform.Application.Configuration.Services;

// 排序键比较：字符串不区分大小写，其余类型按自身顺序。
internal sealed class SortKeyComparer : IComparer<IComparable?>
{
    public static SortKeyComparer Instance { get; } = new();

    public int Compare(IComparable? x, IComparable? y)
    {
        if (x is string left && y is string right)
        {
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        return x is null || y is null ? 0 : x.CompareTo(y);
    }
}
