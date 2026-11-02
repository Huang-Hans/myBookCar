namespace LMS.Dispatch.Application.Credentials.Models;

// 凭据核对结论。DriverId 只在核对通过时有值；不通过时不区分登录标识不存在、密码未设置、密码错误或司机不可登录。
public sealed record DriverCredentialVerification(bool IsVerified, string? DriverId)
{
    public static DriverCredentialVerification Verified(string driverId) => new(true, driverId);

    public static DriverCredentialVerification Rejected() => new(false, null);
}
