using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Commands;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Drivers.Services;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Handlers;

// 新增司机主档。可用状态不由管理端写入，保持为空，等待可信同步来源。
internal sealed class CreateDriverHandler(
    CreateWorkflow workflow,
    IDriverRepository drivers,
    IPlatformDirectory directory,
    BusinessCalendar calendar,
    DriverVehicleBinding vehicleBinding) : ICreateDriverHandler
{
    private const string Operation = "driver.create";
    private const string ResourceType = "driver";

    public async Task<OperationResult<Driver>> HandleAsync(CreateDriverCommand command, CancellationToken cancellationToken)
    {
        var candidate = Driver.Create(
            command.Name, command.LicenseNumber, command.LicenseExpiryDate, command.Phone, command.PropertyId, string.Empty,
            command.Status, command.Languages, command.EmergencyContact, calendar.Today())
            with { AssignedVehicleId = command.AssignedVehicleId };
        var issues = DriverRules.Validate(candidate);
        if (issues.Count > 0)
        {
            return OperationResult<Driver>.Invalid(issues);
        }

        var propertyCheck = await DriverPropertyCheck.CheckAsync<Driver>(
            directory, command.Context.Scope, command.PropertyId, cancellationToken);

        if (propertyCheck is not null) return propertyCheck;
        var binding = await vehicleBinding.ResolveAsync(candidate, command.Context.Scope, cancellationToken);
        return !binding.IsSuccess ? binding
            : await workflow.ExecuteAsync(BuildSpecification(command, binding.Value!), cancellationToken);
    }

    private CreateSpecification<Driver> BuildSpecification(CreateDriverCommand command, Driver candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(candidate),
            driver => driver.DriverId,
            (driver, context, token) => drivers.InsertAsync(driver, context, token),
            (id, token) => drivers.FindByIdAsync(id, token),
            (driver, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, driver.DriverId, now, driver.PropertyId));
}
