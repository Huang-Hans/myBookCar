using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Common;

namespace LMS.Booking.Application.Approvals.Commands;

// 对显式选中的审核作统一决定：全部成功或整体拒绝（合同建议，Q06 待确认）。
public sealed record BulkDecideApprovalsCommand(
    IReadOnlyList<BulkApprovalItem> Items,
    ReviewDecision Decision,
    string Comments,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[]
        {
            Items.Select(item => new object[] { item.ApprovalId, item.ExpectedVersion }).ToList(),
            WireEnum.ToWire(Decision),
            Comments
        });
}
