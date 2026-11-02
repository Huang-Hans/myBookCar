using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Commands;

public sealed record UpdateLocationCommand(
    string LocationId,
    long ExpectedVersion,
    LocationChanges Changes,
    CommandContext Context);
