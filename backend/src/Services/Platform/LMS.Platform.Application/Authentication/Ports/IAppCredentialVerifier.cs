using LMS.BuildingBlocks.Core.Authorization;
using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Application.Authentication.Ports;

// 司机凭据归 Dispatch、客户凭据归 Booking：Platform 不跨库读取 password，只通过受服务身份保护的内部接口核对，且不取回哈希。
public interface IAppCredentialVerifier
{
    Task<CredentialCheck> VerifyAsync(PrincipalType type, string phone, string password, CancellationToken cancellationToken);

    // 刷新时核对对应记录仍存在且可登录。
    Task<CredentialCheck> CheckEligibilityAsync(PrincipalType type, string entityId, CancellationToken cancellationToken);
}
