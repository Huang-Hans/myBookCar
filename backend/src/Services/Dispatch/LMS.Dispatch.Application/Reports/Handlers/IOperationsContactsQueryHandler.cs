using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Reports.Models;

namespace LMS.Dispatch.Application.Reports.Handlers;

public interface IOperationsContactsQueryHandler
{
    Task<OperationResult<OperationsContacts>> GetAsync(string driverId, CancellationToken cancellationToken);
}
