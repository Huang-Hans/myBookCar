using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Application.Files.Models;

namespace LMS.Platform.Application.Files.Handlers;

public interface IDownloadFileContentHandler
{
    Task<OperationResult<FileDownload>> HandleAsync(DownloadFileContentCommand command, CancellationToken cancellationToken);
}
