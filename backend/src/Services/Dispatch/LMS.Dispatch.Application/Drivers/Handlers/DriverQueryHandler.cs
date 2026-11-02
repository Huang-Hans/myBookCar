using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

internal sealed class DriverQueryHandler(IDriverRepository drivers) : IDriverQueryHandler
{
    public async Task<OperationResult<PageResult<Driver>>> ListAsync(ListDriversQuery query, CancellationToken cancellationToken)
    {
        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<Driver>>.Failed(OperationOutcome.Forbidden);
        }

        return OperationResult<PageResult<Driver>>.Succeeded(await drivers.ListAsync(query, cancellationToken));
    }

    public async Task<OperationResult<Driver>> GetAsync(string driverId, PropertyScope scope, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindVisibleAsync(driverId, scope, cancellationToken);

        return driver is null
            ? OperationResult<Driver>.Failed(OperationOutcome.NotFound)
            : OperationResult<Driver>.Succeeded(driver);
    }
}
