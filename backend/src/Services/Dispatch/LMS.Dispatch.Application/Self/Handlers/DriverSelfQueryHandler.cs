using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Self.Services;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Self.Handlers;

internal sealed class DriverSelfQueryHandler(IDriverRepository drivers, DriverAvailabilityReconciler reconciler) : IDriverSelfQueryHandler
{
    public async Task<OperationResult<Driver>> GetAsync(string driverId, CommandContext context, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(driverId, cancellationToken);

        return driver is null
            ? OperationResult<Driver>.Failed(OperationOutcome.NotFound)
            : await reconciler.ReconcileAsync(driver, context, cancellationToken);
    }
}
