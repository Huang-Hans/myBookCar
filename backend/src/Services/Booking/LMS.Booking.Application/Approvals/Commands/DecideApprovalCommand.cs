using System.Text.Json;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Common;

namespace LMS.Booking.Application.Approvals.Commands;

public sealed record DecideApprovalCommand(
    string ApprovalId,
    long ExpectedVersion,
    ReviewDecision Decision,
    string Comments,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() =>
        JsonSerializer.Serialize(new object?[] { ApprovalId, ExpectedVersion, WireEnum.ToWire(Decision), Comments });
}
