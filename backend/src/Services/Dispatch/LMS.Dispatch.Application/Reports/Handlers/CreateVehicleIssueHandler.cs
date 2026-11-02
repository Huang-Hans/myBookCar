using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Inspections.Ports;
using LMS.Dispatch.Application.Reports.Commands;
using LMS.Dispatch.Application.Reports.Ports;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

// 登记车辆问题：司机须启用；可选关联的检查记录必须属于同一司机。服务端只负责真实登记（状态 RECORDED），
// 不会因为插入成功就声称已受理或已维修，也不自动改司机主档或取消行程。经 CreateWorkflow（幂等键 + 记录与审计同事务）。
internal sealed class CreateVehicleIssueHandler(
    CreateWorkflow workflow,
    IDriverRepository drivers,
    IDriverInspectionRepository inspections,
    IVehicleIssueRepository issues,
    TimeProvider timeProvider) : ICreateVehicleIssueHandler
{
    private const string Operation = "vehicle-issue.create";
    private const string ResourceType = "vehicle-issue";

    public async Task<OperationResult<VehicleIssue>> HandleAsync(CreateVehicleIssueCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<VehicleIssue>.Failed(OperationOutcome.NotFound);
        }

        if (driver.Status != DriverStatus.Active)
        {
            return OperationResult<VehicleIssue>.Rejected([new FieldIssue("driver", "Only an active driver can report a vehicle issue")]);
        }

        var rejection = await CheckInspectionAsync(command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command, driver), cancellationToken);
    }

    private async Task<OperationResult<VehicleIssue>?> CheckInspectionAsync(CreateVehicleIssueCommand command, CancellationToken cancellationToken)
    {
        if (command.InspectionId is null)
        {
            return null;
        }

        var inspection = await inspections.FindForDriverAsync(command.InspectionId, command.DriverId, cancellationToken);

        return inspection is null
            ? OperationResult<VehicleIssue>.Invalid([new FieldIssue("inspectionId", "Unknown inspection")])
            : null;
    }

    private CreateSpecification<VehicleIssue> BuildSpecification(CreateVehicleIssueCommand command, Driver driver) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(VehicleIssue.Create(
                driver.DriverId, driver.PropertyId, driver.AssignedVehicle, command.Description, command.InspectionId, timeProvider.GetUtcNow())),
            issue => issue.IssueId,
            (issue, context, token) => issues.InsertAsync(issue, context, token),
            (id, token) => issues.FindByIdAsync(id, token),
            (issue, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, issue.IssueId, now, issue.PropertyId));
}
