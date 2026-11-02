using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Directory;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Reports.Commands;
using LMS.Dispatch.Application.Reports.Ports;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Reports;

namespace LMS.Dispatch.Application.Reports.Handlers;

// 登记事故报告：任何状态的司机都可以报告（安全事件不因主档状态而拒绝记录）。发生时间由司机陈述、登记时间由服务端生成，
// 两者分开保存。可选的行程引用由 Booking 按司机令牌核对归属（不只查进行中的行程，已完成 / 已取消的行程也可补报）。
// 状态固定为 RECORDED：没有运营接入，不推定已转交或已签收。经 CreateWorkflow（幂等键 + 记录与审计同事务）。
internal sealed class CreateIncidentHandler(
    CreateWorkflow workflow,
    IDriverRepository drivers,
    IBookingAssignments bookings,
    IDriverIncidentRepository incidents,
    TimeProvider timeProvider) : ICreateIncidentHandler
{
    private const string Operation = "driver-incident.create";
    private const string ResourceType = "driver-incident";

    public async Task<OperationResult<DriverIncident>> HandleAsync(CreateIncidentCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<DriverIncident>.Failed(OperationOutcome.NotFound);
        }

        var issues = IncidentRules.Validate(command.OccurredAt, timeProvider.GetUtcNow());
        if (issues.Count > 0)
        {
            return OperationResult<DriverIncident>.Invalid(issues);
        }

        var rejection = await CheckTripAsync(command, cancellationToken);

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command, driver), cancellationToken);
    }

    private async Task<OperationResult<DriverIncident>?> CheckTripAsync(CreateIncidentCommand command, CancellationToken cancellationToken)
    {
        if (command.TripId is null)
        {
            return null;
        }

        var trip = await bookings.GetDriverTripAsync(command.TripId, cancellationToken);

        return trip.Outcome switch
        {
            BookingOutcome.Success => null,
            BookingOutcome.NotFound => OperationResult<DriverIncident>.Invalid([new FieldIssue("tripId", "Unknown trip")]),
            BookingOutcome.Forbidden => OperationResult<DriverIncident>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<DriverIncident>.Failed(OperationOutcome.SourceUnavailable)
        };
    }

    private CreateSpecification<DriverIncident> BuildSpecification(CreateIncidentCommand command, Driver driver) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(DriverIncident.Create(
                driver.DriverId, driver.PropertyId, driver.AssignedVehicle, command.Type, command.OccurredAt, command.Description,
                command.TripId, timeProvider.GetUtcNow())),
            incident => incident.IncidentId,
            (incident, context, token) => incidents.InsertAsync(incident, context, token),
            (id, token) => incidents.FindByIdAsync(id, token),
            (incident, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, incident.IncidentId, now, incident.PropertyId));
}
