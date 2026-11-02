namespace LMS.Dispatch.Domain.Shifts.Batches;

public sealed record ShiftBatch(
    string BatchId,
    string Actor,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ShiftBatchItem> Items)
{
    public int CreatedCount => Items.Count(item => !item.IsExisting);
    public int SkippedCount => Items.Count - CreatedCount;
}
