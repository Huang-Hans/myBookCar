namespace LMS.Dispatch.Application.Directory;

// Platform 拥有用户授权与物业配置；Dispatch 只通过它读取，不直接访问 Platform 的数据库。
public interface IPlatformDirectory
{
    Task<CallerLookup> ResolveCallerAsync(CancellationToken cancellationToken);

    Task<PropertyLookup> ContainsPropertyAsync(string propertyId, CancellationToken cancellationToken);
}
