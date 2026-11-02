using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Users.Models;
using LMS.Platform.Domain.Users;

namespace LMS.Platform.Application.Users.Ports;

public interface IPortalUserRepository
{
    Task<PortalUser?> FindByIdAsync(string userId, CancellationToken cancellationToken);

    // 只返回对调用者授权范围可见的用户；不可见与不存在不作区分。
    Task<PortalUser?> FindVisibleAsync(string userId, PropertyScope scope, CancellationToken cancellationToken);

    Task<PortalUser?> FindVisibleAsync(string userId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    // 按 IdP 来源与稳定主体标识查找，用于把已验证令牌关联到应用用户。
    Task<PortalUser?> FindBySubjectAsync(string identityProvider, string subjectId, CancellationToken cancellationToken);

    // 按 IdP 来源与规范化企业邮箱查找尚未关联主体的用户，用于首次登录关联。
    Task<PortalUser?> FindUnlinkedByEmailAsync(string identityProvider, string normalizedEmail, CancellationToken cancellationToken);

    // 可能接收某物业通知的员工候选：已启用、已关联企业身份、角色在给定集合内，且为全物业或属于该物业。
    // 最终的权限与范围一致性判定仍由调用者授权规则完成；最多返回 take 条。
    Task<IReadOnlyList<PortalUser>> ListNotificationCandidatesAsync(
        string propertyId,
        IReadOnlyCollection<string> roleCodes,
        int take,
        CancellationToken cancellationToken);

    Task<PageResult<PortalUser>> ListAsync(ListPortalUsersQuery query, CancellationToken cancellationToken);

    // 企业邮箱重复时抛出 DuplicateRecordException。
    Task InsertAsync(PortalUser user, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。企业邮箱重复时抛出 DuplicateRecordException。
    Task<bool> TryReplaceAsync(PortalUser updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
