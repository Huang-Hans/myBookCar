using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Api.Contracts.Approvals;
using LMS.Booking.Api.Contracts.Qualifications;
using LMS.Booking.Application.Approvals.Models;
using LMS.Booking.Application.Directory;
using LMS.Booking.Domain.Approvals;

namespace LMS.Booking.Api.Mapping.Approvals;

internal static class ApprovalViewMapper
{
    public static CrossBorderApprovalView ToView(CrossBorderApproval approval) => new(
        approval.ApprovalId,
        approval.TripReference,
        new GuestSnapshotView(approval.Guest.GuestId, approval.Guest.Name, approval.Guest.TierCode),
        approval.PropertyId,
        new RouteSnapshotView(approval.Route.PickupName, approval.Route.DropoffName),
        approval.PickupAt.UtcDateTime,
        approval.SlaDeadlineAt?.UtcDateTime,
        approval.AssignedDriverId,
        approval.AssignedVehicle,
        approval.ApprovalChain,
        new LoyaltyReferenceView(
            approval.Loyalty.RedemptionId, approval.Loyalty.PointsRequired, approval.Loyalty.Entitlement, approval.Loyalty.RedemptionState),
        approval.Documents
            .Select(document => new ApprovalDocumentView(WireEnum.ToWire(document.DocumentType), document.IsVerified, document.FileId))
            .ToList(),
        WireEnum.ToWire(approval.Status),
        approval.Decision is null
            ? null
            : new ApprovalDecisionView(
                WireEnum.ToWire(approval.Decision.Decision), approval.Decision.Comments, approval.Decision.DecidedBy, approval.Decision.DecidedAt.UtcDateTime),
        approval.Version);

    public static ApprovalSummaryView ToView(ApprovalSummary summary) => new(summary.PendingCount, summary.ApprovalChain);

    public static ApprovalBulkDecisionView ToView(BulkDecisionResult result) => new(
        result.Items.Select(item => new ApprovalDecisionResultView(item.ApprovalId, WireEnum.ToWire(item.Status), item.Version)).ToList(),
        WireEnum.ToWire(result.Decision));

    public static BlacklistHitView ToView(BlacklistHit hit) =>
        new(hit.ApprovalId, hit.TripReference, WireEnum.ToWire(hit.TargetType), hit.TargetId, hit.Reason, hit.PropertyId);

    public static UploadDescriptionView ToView(FileUploadInfo upload) =>
        new(upload.FileId, new UploadDescription(upload.Url, upload.Method, upload.ContentType, upload.ExpiresAt.UtcDateTime));
}
