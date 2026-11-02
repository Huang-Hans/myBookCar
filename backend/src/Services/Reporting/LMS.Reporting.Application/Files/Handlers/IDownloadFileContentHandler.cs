using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Files.Commands;
using LMS.Reporting.Application.Files.Models;

namespace LMS.Reporting.Application.Files.Handlers;

public interface IDownloadFileContentHandler
{
    Task<OperationResult<FileDownload>> HandleAsync(DownloadFileContentCommand command, CancellationToken cancellationToken);
}
