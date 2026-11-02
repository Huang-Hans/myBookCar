using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

// 行程完成 / 取消后由 Booking 通知释放司机（ON_TRIP → AVAILABLE），写审计。
// 司机记录不保存行程标识，因此以时间判定这次 ON_TRIP 是否属于已结束的行程：ON_TRIP 晚于行程结束才写入，说明司机已经接了新单，不能释放。
// 不是 ON_TRIP（已被本人对账释放、休息、下线）时保持不变，重复通知是幂等的。
internal sealed class ReleaseDriverHandler(
    PatchWorkflow workflow,
    IDriverRepository drivers,
    TimeProvider timeProvider) : IReleaseDriverHandler
{
    private const string Operation = "driver.availability-release";
    private const string ResourceType = "driver";

    public async Task<OperationResult<Driver>> HandleAsync(ReleaseDriverCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<Driver>.Failed(OperationOutcome.NotFound);
        }

        if (!IsHeldByEndedTrip(driver, command.EndedAt))
        {
            return OperationResult<Driver>.Succeeded(driver);
        }

        var released = await workflow.ExecuteAsync(BuildSpecification(command, driver), cancellationToken);

        return released.Outcome == OperationOutcome.StateConflict ? await CurrentAsync(command.DriverId, cancellationToken) : released;
    }

    private PatchSpecification<Driver> BuildSpecification(ReleaseDriverCommand command, Driver driver) =>
        new(
            driver.DriverId,
            driver.Version,
            (id, transaction, token) => drivers.FindVisibleAsync(id, PropertyScope.AllProperties, transaction, token),
            current => current.Version,
            (current, _, _) => Task.FromResult(Release(current, command.EndedAt)),
            (updated, expected, transaction, token) => drivers.TryReplaceAsync(updated, expected, transaction, token),
            (_, after, now) => AuditRecordFactory.Updated(command.Context, Operation, ResourceType, after.DriverId, now, ["availability"], after.PropertyId));

    // 事务内重新判定：读取后状态已变化时返回冲突，不写任何数据。
    private OperationResult<Driver> Release(Driver current, DateTimeOffset endedAt) =>
        IsHeldByEndedTrip(current, endedAt)
            ? OperationResult<Driver>.Succeeded(
                current.WithAvailability(DriverAvailability.Available, DriverAvailabilitySources.TripRelease, timeProvider.GetUtcNow()))
            : OperationResult<Driver>.Failed(OperationOutcome.StateConflict);

    private async Task<OperationResult<Driver>> CurrentAsync(string driverId, CancellationToken cancellationToken)
    {
        var current = await drivers.FindByIdAsync(driverId, cancellationToken);

        return current is null ? OperationResult<Driver>.Failed(OperationOutcome.NotFound) : OperationResult<Driver>.Succeeded(current);
    }

    private static bool IsHeldByEndedTrip(Driver driver, DateTimeOffset endedAt) =>
        driver.Availability == DriverAvailability.OnTrip && (driver.SyncedAt is null || driver.SyncedAt <= endedAt);
}
