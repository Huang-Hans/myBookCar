using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Commands;

public sealed record UpdatePortalUserCommand(
    string UserId,
    long ExpectedVersion,
    PortalUserChanges Changes,
    CommandContext Context);
