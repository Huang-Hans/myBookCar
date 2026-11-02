namespace LMS.Reporting.Domain.Rows;

// 积分引用状态的计数。日报按天存储，跨天合并时按计数相加后再生成汇总文本。
public sealed record RedemptionTally(int Reserved, int Redeemed, int Returned)
{
    public bool IsEmpty => Reserved == 0 && Redeemed == 0 && Returned == 0;

    // 例如 "RESERVED 2 · REDEEMED 1 · RETURNED 0"；全部为 0 时为 null。
    public string? Summary => IsEmpty ? null : $"RESERVED {Reserved} · REDEEMED {Redeemed} · RETURNED {Returned}";
}
