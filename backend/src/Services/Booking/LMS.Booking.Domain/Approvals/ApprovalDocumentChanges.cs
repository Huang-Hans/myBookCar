using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Booking.Domain.Approvals;

// 材料槽的局部更新：绑定文件、变更人工核验结果。FileId 不允许显式清空。
public sealed record ApprovalDocumentChanges(Optional<string> FileId, Optional<bool> IsVerified)
{
    public IReadOnlyList<string> ChangedFields() =>
        ChangedFieldNames.From(("fileId", FileId.IsSet), ("isVerified", IsVerified.IsSet));
}
