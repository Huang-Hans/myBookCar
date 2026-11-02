using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Preferences.Ports;
using LMS.Dispatch.Domain.Preferences;

namespace LMS.Dispatch.Application.Preferences.Handlers;

internal sealed class DriverPreferencesQueryHandler(IDriverRepository drivers, IDriverPreferencesRepository preferences)
    : IDriverPreferencesQueryHandler
{
    public async Task<OperationResult<DriverPreferences>> GetAsync(string driverId, CancellationToken cancellationToken)
    {
        if (await drivers.FindByIdAsync(driverId, cancellationToken) is null)
        {
            return OperationResult<DriverPreferences>.Failed(OperationOutcome.NotFound);
        }

        return OperationResult<DriverPreferences>.Succeeded(
            await preferences.FindAsync(driverId, cancellationToken) ?? DriverPreferences.Default(driverId));
    }
}
