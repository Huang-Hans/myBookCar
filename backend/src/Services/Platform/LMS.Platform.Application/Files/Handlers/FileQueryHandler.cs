using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Application.Files.Ports;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Handlers;

internal sealed class FileQueryHandler(IFileMetadataRepository files) : IFileQueryHandler
{
    // 只返回属于可见物业的文件；没有物业归属的文件（导出结果等）只对全物业调用者可见。不可见按不存在处理。
    public async Task<OperationResult<FileMetadata>> GetAsync(string fileId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var file = await files.FindAsync(fileId, cancellationToken);
        var isVisible = file is not null && (file.PropertyId is null ? scope.IsAllProperties : scope.Allows(file.PropertyId));

        return isVisible ? OperationResult<FileMetadata>.Succeeded(file!) : OperationResult<FileMetadata>.Failed(OperationOutcome.NotFound);
    }
}
