namespace LMS.Booking.Application.Directory;

// Platform 拥有用户授权、物业配置、客户限制和文件元数据；Booking 只通过它读取，不直接访问 Platform 的数据库。
public interface IPlatformDirectory
{
    Task<CallerLookup> ResolveCallerAsync(CancellationToken cancellationToken);

    Task<PropertyLookup> ContainsPropertyAsync(string propertyId, CancellationToken cancellationToken);

    // 返回给定会员中当前存在有效限制的那些。
    Task<DirectoryResult<IReadOnlyList<CustomerRestrictionInfo>>> CheckCustomersAsync(
        IReadOnlyList<string> memberIds,
        CancellationToken cancellationToken);

    Task<DirectoryResult<RegisteredFile>> GetFileAsync(string fileId, CancellationToken cancellationToken);

    Task<DirectoryResult<FileUploadInfo>> RegisterFileAsync(
        RegisterFileRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken);
}
