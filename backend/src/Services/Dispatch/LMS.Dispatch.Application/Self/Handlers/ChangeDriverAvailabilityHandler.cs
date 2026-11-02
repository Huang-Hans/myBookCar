using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Self.Commands;
using LMS.Dispatch.Application.Self.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Self.Handlers;

// 司机本人切换工作状态（AVAILABLE / BREAK / OFF_DUTY）。重复设置同一状态不写入，直接返回当前状态（幂等）；
// 执行中的行程期间、停用 / 暂停的司机不允许切换，规则见 DriverSelfRules。
internal sealed class ChangeDriverAvailabilityHandler(
    PatchWorkflow workflow,
    IDriverRepository drivers,
    DriverAvailabilityReconciler reconciler,
    TimeProvider timeProvider) : IChangeDriverAvailabilityHandler
{
    private const string Operation = "driver.availability-change";
    private const string ResourceType = "driver";

    public async Task<OperationResult<Driver>> HandleAsync(ChangeAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<Driver>.Failed(OperationOutcome.NotFound);
        }

        var reconciled = await reconciler.ReconcileAsync(driver, command.Context, cancellationToken);

        return reconciled.IsSuccess ? await ChangeAsync(reconciled.Value!, command, cancellationToken) : reconciled;
    }

    private async Task<OperationResult<Driver>> ChangeAsync(Driver driver, ChangeAvailabilityCommand command, CancellationToken cancellationToken)
    {
        if (driver.Availability == command.Target)
        {
            return OperationResult<Driver>.Succeeded(driver);
        }

        var issue = DriverSelfRules.CheckAvailabilityChange(driver, command.Target);

        return issue is not null
            ? OperationResult<Driver>.Rejected([issue])
            : await workflow.ExecuteAsync(BuildSpecification(driver, command), cancellationToken);
    }

    private PatchSpecification<Driver> BuildSpecification(Driver driver, ChangeAvailabilityCommand command) =>
        new(
            driver.DriverId,
            driver.Version,
            (id, transaction, token) => drivers.FindVisibleAsync(id, PropertyScope.AllProperties, transaction, token),
            current => current.Version,
            (current, _, _) => Task.FromResult(Apply(current, command)),
            (updated, expected, transaction, token) => drivers.TryReplaceAsync(updated, expected, transaction, token),
            (_, after, now) => AuditRecordFactory.Updated(command.Context, Operation, ResourceType, after.DriverId, now, ["availability"], after.PropertyId));

    // 事务内按最新状态再次校验，避免读取之后司机被接单等并发改变。
    private OperationResult<Driver> Apply(Driver current, ChangeAvailabilityCommand command)
    {
        var issue = DriverSelfRules.CheckAvailabilityChange(current, command.Target);

        return issue is not null
            ? OperationResult<Driver>.Rejected([issue])
            : OperationResult<Driver>.Succeeded(
                current.WithAvailability(command.Target, DriverAvailabilitySources.SelfReported, timeProvider.GetUtcNow()));
    }
}
