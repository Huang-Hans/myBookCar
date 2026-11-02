using LMS.Dispatch.Domain.Shifts.Batches;

namespace LMS.Dispatch.Application.Shifts.Models;

public sealed record ShiftBatchPreview(
    string PreviewHash,
    int DriverCount,
    int DateCount,
    IReadOnlyList<ShiftBatchItem> Items)
{
    public int CreateCount => Items.Count(item => !item.IsExisting);
    public int ExistingCount => Items.Count - CreateCount;
}
