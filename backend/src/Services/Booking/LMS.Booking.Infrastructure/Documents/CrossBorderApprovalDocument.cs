using LMS.BuildingBlocks.Core.Wire;
using LMS.BuildingBlocks.MongoDB.Concurrency;
using LMS.Booking.Domain.Approvals;
using LMS.Booking.Infrastructure.Persistence;
using MongoDB.Bson.Serialization.Attributes;

namespace LMS.Booking.Infrastructure.Documents;

internal sealed class CrossBorderApprovalDocument
{
    [BsonId]
    public string ApprovalId { get; init; } = string.Empty;

    [BsonElement("tripReference")]
    public string TripReference { get; init; } = string.Empty;

    [BsonElement("guest")]
    public GuestSnapshotDocument Guest { get; init; } = new();

    [BsonElement("propertyId")]
    public string PropertyId { get; init; } = string.Empty;

    [BsonElement("route")]
    public RouteSnapshotDocument Route { get; init; } = new();

    [BsonElement("pickupAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime PickupAt { get; init; }

    [BsonElement("slaDeadlineAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? SlaDeadlineAt { get; init; }

    [BsonElement("assignedDriverId")]
    public string? AssignedDriverId { get; init; }

    [BsonElement("assignedVehicle")]
    public string? AssignedVehicle { get; init; }

    [BsonElement("approvalChain")]
    public List<string> ApprovalChain { get; init; } = [];

    [BsonElement("loyalty")]
    public LoyaltyReferenceDocument Loyalty { get; init; } = new();

    [BsonElement("documents")]
    public List<ApprovalDocumentSlotDocument> Documents { get; init; } = [];

    [BsonElement("status")]
    public string Status { get; init; } = string.Empty;

    [BsonElement("decision")]
    public ApprovalDecisionDocument? Decision { get; init; }

    [BsonElement(VersionedUpdate.VersionField)]
    public long Version { get; init; }

    public static CrossBorderApprovalDocument FromDomain(CrossBorderApproval approval) => new()
    {
        ApprovalId = approval.ApprovalId,
        TripReference = approval.TripReference,
        Guest = GuestSnapshotDocument.FromDomain(approval.Guest),
        PropertyId = approval.PropertyId,
        Route = RouteSnapshotDocument.FromDomain(approval.Route),
        PickupAt = UtcDateTimes.ToStored(approval.PickupAt),
        SlaDeadlineAt = approval.SlaDeadlineAt is null ? null : UtcDateTimes.ToStored(approval.SlaDeadlineAt.Value),
        AssignedDriverId = approval.AssignedDriverId,
        AssignedVehicle = approval.AssignedVehicle,
        ApprovalChain = approval.ApprovalChain.ToList(),
        Loyalty = LoyaltyReferenceDocument.FromDomain(approval.Loyalty),
        Documents = approval.Documents.Select(ApprovalDocumentSlotDocument.FromDomain).ToList(),
        Status = WireEnum.ToWire(approval.Status),
        Decision = approval.Decision is null ? null : ApprovalDecisionDocument.FromDomain(approval.Decision),
        Version = approval.Version
    };

    public CrossBorderApproval ToDomain() => new(
        ApprovalId,
        TripReference,
        Guest.ToDomain(),
        PropertyId,
        Route.ToDomain(),
        UtcDateTimes.FromStored(PickupAt),
        SlaDeadlineAt is null ? null : UtcDateTimes.FromStored(SlaDeadlineAt.Value),
        AssignedDriverId,
        AssignedVehicle,
        ApprovalChain,
        Loyalty.ToDomain(),
        Documents.Select(document => document.ToDomain()).ToList(),
        StoredValues.ParseEnum<ApprovalStatus>(Status, "status"),
        Decision?.ToDomain(),
        Version);
}
