using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Exports.Commands;
using LMS.Platform.Domain.Exports;

namespace LMS.Platform.Application.Exports.Handlers;

public interface ICreateAuditExportHandler
{
    Task<OperationResult<AuditExport>> HandleAsync(CreateAuditExportCommand command, CancellationToken cancellationToken);
}
