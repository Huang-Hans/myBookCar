namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed record ShiftBatchView(string BatchId, string CreatedAt, int CreatedCount, int SkippedCount,
    IReadOnlyList<ShiftBatchItemView> Items);
