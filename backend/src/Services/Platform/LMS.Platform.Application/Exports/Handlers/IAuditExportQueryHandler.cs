using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Application.Exports.Models;
using LMS.Platform.Domain.Exports;

namespace LMS.Platform.Application.Exports.Handlers;

public interface IAuditExportQueryHandler
{
    // 按调用者当前授权范围读取导出；范围之外或已不存在按不可见处理。
    Task<OperationResult<AuditExport>> GetAsync(string exportId, PropertyScope scope, CancellationToken cancellationToken);

    // 为已完成且未过期的导出签发短效下载描述；签发时按当前授权复核。
    Task<OperationResult<ExportDownload>> DescribeDownloadAsync(string exportId, PropertyScope scope, CancellationToken cancellationToken);
}
