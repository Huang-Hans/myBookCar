using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Application.Drivers.Ports;

namespace LMS.Dispatch.Application.Drivers.Handlers;

internal sealed class CheckDriverStatusesHandler(IDriverRepository drivers) : ICheckDriverStatusesHandler
{
    public async Task<IReadOnlyList<DriverStatusInfo>> HandleAsync(
        IReadOnlyList<string> driverIds,
        PropertyScope scope,
        CancellationToken cancellationToken)
    {
        if (driverIds.Count == 0)
        {
            return [];
        }

        var found = await drivers.FindVisibleByIdsAsync(driverIds, scope, cancellationToken);

        return found.Select(driver => new DriverStatusInfo(driver.DriverId, driver.PropertyId, driver.Status, driver.Restriction?.Reason)).ToList();
    }
}
