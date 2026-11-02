namespace LMS.Reporting.Infrastructure.Platform;

internal sealed record PlatformCallerPayload(
    string UserId,
    string RoleCode,
    bool HasAllPropertyAccess,
    string? PropertyId,
    List<string> Permissions);
