using LMS.Booking.Domain.Common;

namespace LMS.Booking.Domain.Approvals;

// 跨境审批：队列快照、四个材料槽和最终决定。ApprovalChain 只是展示用的阶段标签，不用于授权（Q06）。
public sealed record CrossBorderApproval(
    string ApprovalId,
    string TripReference,
    Qualifications.GuestSnapshot Guest,
    string PropertyId,
    RouteSnapshot Route,
    DateTimeOffset PickupAt,
    DateTimeOffset? SlaDeadlineAt,
    string? AssignedDriverId,
    string? AssignedVehicle,
    IReadOnlyList<string> ApprovalChain,
    LoyaltyReference Loyalty,
    IReadOnlyList<ApprovalDocument> Documents,
    ApprovalStatus Status,
    ApprovalDecision? Decision,
    long Version)
{
    public bool AreAllDocumentsVerified => Documents.All(document => document.IsVerified);

    public ApprovalDocument SlotOf(ApprovalDocumentType documentType) => Documents.Single(document => document.DocumentType == documentType);

    // 绑定了不同的文件后，之前的人工核验不再适用，除非同一请求明确重新核验。
    public CrossBorderApproval ApplyDocument(ApprovalDocumentType documentType, ApprovalDocumentChanges changes)
    {
        var slot = SlotOf(documentType);
        var fileId = changes.FileId.IsSet ? changes.FileId.Value : slot.FileId;
        var fileChanged = changes.FileId.IsSet && fileId != slot.FileId;
        var isVerified = changes.IsVerified.IsSet ? changes.IsVerified.Value : !fileChanged && slot.IsVerified;
        var updated = new ApprovalDocument(documentType, isVerified, fileId);

        return this with { Documents = Documents.Select(document => document.DocumentType == documentType ? updated : document).ToList() };
    }

    public CrossBorderApproval Decide(ReviewDecision decision, string comments, string decidedBy, DateTimeOffset decidedAt) =>
        this with
        {
            Status = decision == ReviewDecision.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected,
            Decision = new ApprovalDecision(decision, comments, decidedBy, decidedAt)
        };
}
