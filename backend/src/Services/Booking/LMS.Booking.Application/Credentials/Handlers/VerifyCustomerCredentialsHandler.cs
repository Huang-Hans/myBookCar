using LMS.BuildingBlocks.Core.Security;
using LMS.Booking.Application.Credentials.Models;
using LMS.Booking.Application.Credentials.Ports;

namespace LMS.Booking.Application.Credentials.Handlers;

// 核对客户登录凭据：手机号规范化后必须唯一匹配一条宾客记录，记录已设置密码且哈希校验通过。
// 密码为空的记录不能登录，没有任何“仅凭手机号”的替代路径；密码原样校验，不 trim、不转大小写。
// 登录不检查积分、游玩、免费用车资格或配额，这些判断留在预订业务里。
internal sealed class VerifyCustomerCredentialsHandler(ICustomerCredentialReader credentials, ICredentialHasher hasher)
    : IVerifyCustomerCredentialsHandler
{
    public async Task<CustomerCredentialVerification> HandleAsync(string phone, string password, CancellationToken cancellationToken)
    {
        if (!PhoneNumber.TryNormalize(phone, out var normalizedPhone))
        {
            return CustomerCredentialVerification.Rejected();
        }

        var credential = await credentials.FindByPhoneAsync(normalizedPhone, cancellationToken);

        return credential is not null && hasher.Verify(credential.PasswordHash, password)
            ? CustomerCredentialVerification.Verified(credential.GuestId)
            : CustomerCredentialVerification.Rejected();
    }
}
