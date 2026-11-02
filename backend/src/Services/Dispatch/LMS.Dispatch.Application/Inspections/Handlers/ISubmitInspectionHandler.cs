using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Inspections.Commands;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Handlers;

public interface ISubmitInspectionHandler
{
    Task<OperationResult<DriverInspection>> HandleAsync(SubmitInspectionCommand command, CancellationToken cancellationToken);
}
