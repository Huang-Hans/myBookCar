using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Handlers;

public interface ICurrentUserHandler
{
    // 读取当前令牌所属调用者自己的应用用户档案；用户标识来自已验证的授权，不来自请求参数。
    Task<OperationResult<PortalUser>> GetAsync(string userId, CancellationToken cancellationToken);
}
