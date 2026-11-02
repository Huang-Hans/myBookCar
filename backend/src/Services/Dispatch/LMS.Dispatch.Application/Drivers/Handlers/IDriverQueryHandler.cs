using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

public interface IDriverQueryHandler
{
    Task<OperationResult<PageResult<Driver>>> ListAsync(ListDriversQuery query, CancellationToken cancellationToken);

    Task<OperationResult<Driver>> GetAsync(string driverId, PropertyScope scope, CancellationToken cancellationToken);
}
