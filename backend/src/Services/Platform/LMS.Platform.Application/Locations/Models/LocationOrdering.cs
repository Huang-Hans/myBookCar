using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Models;

public sealed record LocationOrdering(LocationRegion Region, IReadOnlyList<LocationOrderResult> Items);
