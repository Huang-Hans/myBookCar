namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>Create a property in the active directory. Daily quota is managed by Booking.</summary>
public sealed record CreatePropertyRequest(
    string? PropertyId,
    string? Name,
    string? Address,
    string? ContactPerson,
    string? ContactPhone,
    int? AssignedFleetCount,
    string? OperatingHours,
    string? ServiceLevel,
    int? MinimumLeadTimeHours,
    string? CancellationPolicyReference,
    string? MembershipEligibility,
    string? VipOverrideRule,
    bool? IsActive);
