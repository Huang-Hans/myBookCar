using LMS.BuildingBlocks.Core.Patching;

namespace LMS.Platform.Domain.Locations;

public sealed record CoordinatesChanges(Optional<double> Latitude, Optional<double> Longitude)
{
    public Coordinates ApplyTo(Coordinates current) =>
        new(Latitude.ValueOr(current.Latitude), Longitude.ValueOr(current.Longitude));
}
