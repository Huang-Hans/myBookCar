using LMS.Dispatch.Application.Credentials.Models;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Credentials.Handlers;

internal static class DriverLoginRules
{
    // 已设置密码，且司机为 ACTIVE。INACTIVE / SUSPENDED 的司机不签发也不刷新令牌。
    public static bool IsLoginAllowed(DriverCredential credential) =>
        !string.IsNullOrEmpty(credential.PasswordHash) && credential.Status == DriverStatus.Active;
}
