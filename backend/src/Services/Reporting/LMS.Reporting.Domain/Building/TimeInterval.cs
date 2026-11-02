namespace LMS.Reporting.Domain.Building;

// 半开时间区间 [Start, End)。结束不晚于开始的区间长度为 0。
public readonly record struct TimeInterval(DateTimeOffset Start, DateTimeOffset End)
{
    public TimeSpan Duration => End > Start ? End - Start : TimeSpan.Zero;

    public TimeSpan OverlapWith(TimeInterval other)
    {
        var start = Start > other.Start ? Start : other.Start;
        var end = End < other.End ? End : other.End;

        return end > start ? end - start : TimeSpan.Zero;
    }

    // 合并重叠或相接的区间，返回按开始时间排序、互不重叠的区间。
    public static IReadOnlyList<TimeInterval> Merge(IEnumerable<TimeInterval> intervals)
    {
        var merged = new List<TimeInterval>();
        foreach (var interval in intervals.Where(candidate => candidate.Duration > TimeSpan.Zero).OrderBy(candidate => candidate.Start))
        {
            if (merged.Count > 0 && interval.Start <= merged[^1].End)
            {
                merged[^1] = merged[^1] with { End = interval.End > merged[^1].End ? interval.End : merged[^1].End };
                continue;
            }

            merged.Add(interval);
        }

        return merged;
    }
}
