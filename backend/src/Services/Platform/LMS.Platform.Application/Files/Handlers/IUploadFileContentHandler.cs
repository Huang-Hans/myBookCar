using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Handlers;

public interface IUploadFileContentHandler
{
    Task<OperationResult<FileMetadata>> HandleAsync(UploadFileContentCommand command, CancellationToken cancellationToken);
}
