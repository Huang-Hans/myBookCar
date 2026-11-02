using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Domain.Approvals;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class ApprovalDocumentSlotDocument
{
    [BsonElement("documentType")]
    public string DocumentType { get; init; } = string.Empty;

    [BsonElement("isVerified")]
    public bool IsVerified { get; init; }

    [BsonElement("fileId")]
    public string? FileId { get; init; }

    public static ApprovalDocumentSlotDocument FromDomain(ApprovalDocument document) => new()
    {
        DocumentType = WireEnum.ToWire(document.DocumentType),
        IsVerified = document.IsVerified,
        FileId = document.FileId
    };

    public ApprovalDocument ToDomain() =>
        new(StoredValues.ParseEnum<ApprovalDocumentType>(DocumentType, "documents.documentType"), IsVerified, FileId);
}
