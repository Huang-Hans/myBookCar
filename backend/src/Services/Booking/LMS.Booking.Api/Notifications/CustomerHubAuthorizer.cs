using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Booking.Application.Credentials.Handlers;

namespace LMS.Booking.Api.Notifications;

// 客户 Hub 的授权：令牌验签之外，还要确认本地客户记录仍存在且仍允许登录（已设置密码），
// 不能把“仅验签通过”当作已取得实体状态授权。
internal sealed class CustomerHubAuthorizer(IPrincipalRecordVerifier verifier, ICheckCustomerLoginEligibilityHandler eligibility)
{
    public async Task<bool> IsUsableAsync(string guestId, CancellationToken cancellationToken) =>
        await verifier.IsUsableAsync(PrincipalType.Customer, guestId, cancellationToken)
        && await eligibility.HandleAsync(guestId, cancellationToken);
}
