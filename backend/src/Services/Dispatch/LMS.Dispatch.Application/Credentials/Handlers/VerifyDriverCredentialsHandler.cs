using LMS.BuildingBlocks.Core.Security;
using LMS.Dispatch.Application.Credentials.Models;
using LMS.Dispatch.Application.Credentials.Ports;

namespace LMS.Dispatch.Application.Credentials.Handlers;

// 核对司机登录凭据：手机号规范化后必须唯一匹配一条司机记录，记录已设置密码且哈希校验通过，并且司机为 ACTIVE。
// 密码为空的记录不能登录，没有任何“仅凭手机号”的替代路径；密码原样校验，不 trim、不转大小写。
internal sealed class VerifyDriverCredentialsHandler(IDriverCredentialReader credentials, ICredentialHasher hasher)
    : IVerifyDriverCredentialsHandler
{
    public async Task<DriverCredentialVerification> HandleAsync(string phone, string password, CancellationToken cancellationToken)
    {
        if (!PhoneNumber.TryNormalize(phone, out var normalizedPhone))
        {
            return DriverCredentialVerification.Rejected();
        }

        var credential = await credentials.FindByPhoneAsync(normalizedPhone, cancellationToken);

        return credential is not null && DriverLoginRules.IsLoginAllowed(credential) && hasher.Verify(credential.PasswordHash, password)
            ? DriverCredentialVerification.Verified(credential.DriverId)
            : DriverCredentialVerification.Rejected();
    }
}
