using LMS.BuildingBlocks.Core.Authorization;
using LMS.Platform.Application.Authentication.Models;
using LMS.Platform.Application.Authentication.Ports;

namespace LMS.Platform.Infrastructure.Authentication;

// 按登录入口的身份类型把核对交给数据所属服务：司机只问 Dispatch，客户只问 Booking，不跨表、不合并。
internal sealed class AppCredentialVerifier(DispatchCredentialClient drivers, BookingCredentialClient customers) : IAppCredentialVerifier
{
    public Task<CredentialCheck> VerifyAsync(PrincipalType type, string phone, string password, CancellationToken cancellationToken) =>
        ClientFor(type).VerifyAsync(phone, password, cancellationToken);

    public Task<CredentialCheck> CheckEligibilityAsync(PrincipalType type, string entityId, CancellationToken cancellationToken) =>
        ClientFor(type).CheckEligibilityAsync(entityId, cancellationToken);

    private CredentialServiceClient ClientFor(PrincipalType type) => type switch
    {
        PrincipalType.Driver => drivers,
        PrincipalType.Customer => customers,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown principal type.")
    };
}
