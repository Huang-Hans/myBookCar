namespace LMS.Dispatch.Api.Contracts.Shifts;

public sealed record ShiftBatchPreviewView(string PreviewHash, int DriverCount, int DateCount,
    int CreateCount, int ExistingCount, bool CanSubmit, IReadOnlyList<ShiftBatchItemView> Items);
