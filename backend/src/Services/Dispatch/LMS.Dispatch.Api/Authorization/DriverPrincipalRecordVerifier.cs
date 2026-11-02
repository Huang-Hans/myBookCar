using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Identity.Authorization;
using LMS.Dispatch.Application.Drivers.Ports;

namespace LMS.Dispatch.Api.Authorization;

// 司机令牌的 sub 必须对应 Dispatch 司机表中仍存在的记录。停用/暂停等业务状态不在这里判断：
// 它们由接单、状态变更等用例按司机表的实时状态约束，不因访问令牌尚未过期而绕过。
internal sealed class DriverPrincipalRecordVerifier(IDriverRepository drivers) : IPrincipalRecordVerifier
{
    public async Task<bool> IsUsableAsync(PrincipalType type, string entityId, CancellationToken cancellationToken) =>
        type == PrincipalType.Driver && await drivers.FindByIdAsync(entityId, cancellationToken) is not null;
}
