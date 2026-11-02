namespace LMS.Booking.Infrastructure.Remote;

internal sealed record RemoteCallerPayload(
    string UserId,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    List<string> Permissions,
    string? Name = null);
