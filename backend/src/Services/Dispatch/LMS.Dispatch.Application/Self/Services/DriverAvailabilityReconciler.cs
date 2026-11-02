using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Self.Services;

// 司机状态与权威履约事实的对账：存储为 ON_TRIP 时向 Booking 核对本人是否仍有有效行程。
// 行程已完成 / 取消 / 转派而状态尚未释放时（跨服务同步失败或未触发），恢复为 AVAILABLE 并写审计；
// Booking 暂时不可用时不猜测，按来源不可用返回，避免把过期状态当作事实。
internal sealed class DriverAvailabilityReconciler(
    PatchWorkflow workflow,
    IDriverRepository drivers,
    IBookingAssignments bookings,
    TimeProvider timeProvider)
{
    private const string Operation = "driver.availability-reconcile";
    private const string ResourceType = "driver";

    public async Task<OperationResult<Driver>> ReconcileAsync(Driver driver, CommandContext context, CancellationToken cancellationToken)
    {
        if (driver.Availability != DriverAvailability.OnTrip)
        {
            return OperationResult<Driver>.Succeeded(driver);
        }

        var active = await bookings.GetActiveTripAsync(cancellationToken);

        return active.Outcome switch
        {
            BookingOutcome.Success => OperationResult<Driver>.Succeeded(driver),
            BookingOutcome.NotFound => await ReleaseAsync(driver, context, cancellationToken),
            _ => OperationResult<Driver>.Failed(OperationOutcome.SourceUnavailable)
        };
    }

    private Task<OperationResult<Driver>> ReleaseAsync(Driver driver, CommandContext context, CancellationToken cancellationToken) =>
        workflow.ExecuteAsync(
            new PatchSpecification<Driver>(
                driver.DriverId,
                driver.Version,
                (id, transaction, token) => drivers.FindVisibleAsync(id, PropertyScope.AllProperties, transaction, token),
                current => current.Version,
                (current, _, _) => Task.FromResult(OperationResult<Driver>.Succeeded(Release(current))),
                (updated, expected, transaction, token) => drivers.TryReplaceAsync(updated, expected, transaction, token),
                (_, after, now) => AuditRecordFactory.Updated(context, Operation, ResourceType, after.DriverId, now, ["availability"], after.PropertyId)),
            cancellationToken);

    private Driver Release(Driver current) =>
        current.Availability == DriverAvailability.OnTrip
            ? current.WithAvailability(DriverAvailability.Available, DriverAvailabilitySources.TripReconciliation, timeProvider.GetUtcNow())
            : current;
}
