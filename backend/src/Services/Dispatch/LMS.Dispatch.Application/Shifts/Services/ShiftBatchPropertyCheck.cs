using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Shifts.Models;

namespace LMS.Dispatch.Application.Shifts.Services;

internal sealed class ShiftBatchPropertyCheck(IDriverRepository drivers, IPlatformDirectory directory)
{
    public async Task<OperationResult<T>?> CheckAsync<T>(ShiftBatchRules rules, PropertyScope scope,
        CancellationToken cancellationToken)
    {
        var current = await drivers.FindVisibleByIdsAsync(rules.DriverIds, scope, null, cancellationToken);
        var properties = current.Select(driver => driver.PropertyId).ToHashSet(StringComparer.Ordinal);
        if (rules.PropertyId is not null) properties.Add(rules.PropertyId);
        return await CheckPropertiesAsync<T>(properties, scope, cancellationToken);
    }

    private async Task<OperationResult<T>?> CheckPropertiesAsync<T>(IEnumerable<string> properties,
        PropertyScope scope, CancellationToken cancellationToken)
    {
        foreach (var propertyId in properties)
        {
            var failure = await DriverPropertyCheck.CheckAsync<T>(directory, scope, propertyId, cancellationToken);
            if (failure is not null) return failure;
        }
        return null;
    }
}
