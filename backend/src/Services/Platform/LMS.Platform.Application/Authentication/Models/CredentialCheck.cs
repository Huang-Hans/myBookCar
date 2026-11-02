namespace LMS.Platform.Application.Authentication.Models;

// 所属服务（Dispatch / Booking）对登录凭据或登录资格的核对结果；EntityId 为已验证的 driverId / guestId。
public sealed record CredentialCheck(CredentialCheckOutcome Outcome, string? EntityId)
{
    public static CredentialCheck Verified(string entityId) => new(CredentialCheckOutcome.Verified, entityId);

    public static CredentialCheck Rejected() => new(CredentialCheckOutcome.Rejected, null);

    public static CredentialCheck Unavailable() => new(CredentialCheckOutcome.Unavailable, null);
}
