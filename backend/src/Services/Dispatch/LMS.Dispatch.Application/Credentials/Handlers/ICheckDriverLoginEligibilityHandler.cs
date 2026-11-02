namespace LMS.Dispatch.Application.Credentials.Handlers;

public interface ICheckDriverLoginEligibilityHandler
{
    // 司机记录仍存在、已设置密码且为 ACTIVE 时为 true。刷新令牌前由 Platform 调用。
    Task<bool> HandleAsync(string driverId, CancellationToken cancellationToken);
}
