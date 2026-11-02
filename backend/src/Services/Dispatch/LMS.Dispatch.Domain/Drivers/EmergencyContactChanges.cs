using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Dispatch.Domain.Drivers;

public sealed record EmergencyContactChanges(Optional<string> Name, Optional<string> Phone)
{
    public EmergencyContact ApplyTo(EmergencyContact current) =>
        new(Name.ValueOr(current.Name), Phone.ValueOr(current.Phone));
}
