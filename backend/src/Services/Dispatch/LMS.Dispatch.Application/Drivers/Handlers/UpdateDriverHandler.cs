using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

// 编辑或停用司机。仅 status=INACTIVE 表达停用；停用保留记录，并登记停用限制。
internal sealed class UpdateDriverHandler(
    PatchWorkflow workflow,
    IDriverRepository drivers,
    IPlatformDirectory directory,
    BusinessCalendar calendar,
    DriverVehicleBinding vehicleBinding) : IUpdateDriverHandler
{
    private const string Operation = "driver.update";
    private const string ResourceType = "driver";

    public async Task<OperationResult<Driver>> HandleAsync(UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        var rejection = await CheckPropertyChangeAsync(command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private async Task<OperationResult<Driver>?> CheckPropertyChangeAsync(UpdateDriverCommand command, CancellationToken cancellationToken)
    {
        var propertyId = command.Changes.PropertyId;

        return propertyId.IsSet && propertyId.Value is not null
            ? await DriverPropertyCheck.CheckAsync<Driver>(directory, command.Context.Scope, propertyId.Value, cancellationToken)
            : null;
    }

    private PatchSpecification<Driver> BuildSpecification(UpdateDriverCommand command)
    {
        var businessDate = calendar.Today();

        return new PatchSpecification<Driver>(
            command.DriverId,
            command.ExpectedVersion,
            (id, context, token) => drivers.FindVisibleAsync(id, command.Context.Scope, context, token),
            driver => driver.Version,
            (driver, _, token) => MergeAsync(driver, command, businessDate, token),
            (updated, expected, context, token) => drivers.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.DriverId, now, command.Changes.ChangedFields(), after.PropertyId));
    }

    private async Task<OperationResult<Driver>> MergeAsync(Driver current, UpdateDriverCommand command,
        DateOnly businessDate, CancellationToken cancellationToken)
    {
        var merged = current.Apply(command.Changes, businessDate);
        var issues = DriverRules.Validate(merged);

        if (issues.Count > 0) return OperationResult<Driver>.Invalid(issues);
        return command.Changes.AssignedVehicleId.IsSet || command.Changes.PropertyId.IsSet
            ? await vehicleBinding.ResolveAsync(merged, command.Context.Scope, cancellationToken)
            : OperationResult<Driver>.Succeeded(merged);
    }
}
