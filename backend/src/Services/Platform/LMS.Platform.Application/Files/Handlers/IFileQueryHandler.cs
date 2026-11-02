using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Platform.Domain.Files;

namespace LMS.Platform.Application.Files.Handlers;

public interface IFileQueryHandler
{
    Task<OperationResult<FileMetadata>> GetAsync(string fileId, PropertyScope scope, CancellationToken cancellationToken);
}
