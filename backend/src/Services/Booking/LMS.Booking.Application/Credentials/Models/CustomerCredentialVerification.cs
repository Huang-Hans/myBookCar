namespace LMS.Booking.Application.Credentials.Models;

// 凭据核对结论。GuestId 只在核对通过时有值；不通过时不区分会员号不存在、密码未设置或密码错误。
public sealed record CustomerCredentialVerification(bool IsVerified, string? GuestId)
{
    public static CustomerCredentialVerification Verified(string guestId) => new(true, guestId);

    public static CustomerCredentialVerification Rejected() => new(false, null);
}
