using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Files.Commands;
using LMS.Platform.Application.Files.Models;

namespace LMS.Platform.Application.Files.Handlers;

public interface IRegisterFileHandler
{
    Task<OperationResult<FileRegistration>> HandleAsync(RegisterFileCommand command, CancellationToken cancellationToken);
}
