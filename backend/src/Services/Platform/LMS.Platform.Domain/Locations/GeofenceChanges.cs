using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Domain.Locations;

public sealed record GeofenceChanges(Optional<bool> IsEnabled, Optional<double> RadiusMeters)
{
    public Geofence ApplyTo(Geofence current) =>
        new(IsEnabled.ValueOr(current.IsEnabled), RadiusMeters.ValueOr(current.RadiusMeters));
}
