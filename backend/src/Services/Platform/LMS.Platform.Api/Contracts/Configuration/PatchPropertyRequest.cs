using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>Merge patch; propertyId is immutable and dailyQuota is not writable.</summary>
public sealed record PatchPropertyRequest(
    Optional<string?> Name,
    Optional<string?> Address,
    Optional<string?> ContactPerson,
    Optional<string?> ContactPhone,
    Optional<int?> AssignedFleetCount,
    Optional<string?> OperatingHours,
    Optional<string?> ServiceLevel,
    Optional<int?> MinimumLeadTimeHours,
    Optional<string?> CancellationPolicyReference,
    Optional<string?> MembershipEligibility,
    Optional<string?> VipOverrideRule,
    Optional<bool?> IsActive);
