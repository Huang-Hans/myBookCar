using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Application.Configuration.Models;

public sealed record PropertyChanges(
    Optional<string> Name,
    Optional<string> Address,
    Optional<string> ContactPerson,
    Optional<string> ContactPhone,
    Optional<int> AssignedFleetCount,
    Optional<string> OperatingHours,
    Optional<string> ServiceLevel,
    Optional<int> MinimumLeadTimeHours,
    Optional<string> CancellationPolicyReference,
    Optional<string> MembershipEligibility,
    Optional<string> VipOverrideRule,
    Optional<bool> IsActive)
{
    public IReadOnlyList<string> ChangedFields() => ChangedFieldNames.From(
        ("name", Name.IsSet),
        ("address", Address.IsSet),
        ("contactPerson", ContactPerson.IsSet),
        ("contactPhone", ContactPhone.IsSet),
        ("assignedFleetCount", AssignedFleetCount.IsSet),
        ("operatingHours", OperatingHours.IsSet),
        ("serviceLevel", ServiceLevel.IsSet),
        ("minimumLeadTimeHours", MinimumLeadTimeHours.IsSet),
        ("cancellationPolicyReference", CancellationPolicyReference.IsSet),
        ("membershipEligibility", MembershipEligibility.IsSet),
        ("vipOverrideRule", VipOverrideRule.IsSet),
        ("isActive", IsActive.IsSet));

    public ConfiguredProperty Apply(ConfiguredProperty current) => current with
    {
        Name = Name.ValueOr(current.Name),
        Address = Address.ValueOr(current.Address),
        ContactPerson = ContactPerson.ValueOr(current.ContactPerson),
        ContactPhone = ContactPhone.ValueOr(current.ContactPhone),
        AssignedFleetCount = AssignedFleetCount.ValueOr(current.AssignedFleetCount),
        OperatingHours = OperatingHours.ValueOr(current.OperatingHours),
        ServiceLevel = ServiceLevel.ValueOr(current.ServiceLevel),
        MinimumLeadTimeHours = MinimumLeadTimeHours.ValueOr(current.MinimumLeadTimeHours),
        CancellationPolicyReference = CancellationPolicyReference.ValueOr(current.CancellationPolicyReference),
        MembershipEligibility = MembershipEligibility.ValueOr(current.MembershipEligibility),
        VipOverrideRule = VipOverrideRule.ValueOr(current.VipOverrideRule),
        IsActive = IsActive.ValueOr(current.IsActive),
        Version = current.Version + 1
    };
}
