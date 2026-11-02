using LMS.BuildingBlocks.Core.Operations;
using LMS.Dispatch.Application.Auditing.Services;
using LMS.Dispatch.Application.Drivers.Ports;
using LMS.Dispatch.Application.Inspections.Commands;
using LMS.Dispatch.Application.Inspections.Ports;
using LMS.Dispatch.Application.Workflows;
using LMS.Dispatch.Domain.Drivers;
using LMS.Dispatch.Domain.Inspections;

namespace LMS.Dispatch.Application.Inspections.Handlers;

// 提交检查：司机须启用；模板版本与必填项完整性在服务端校验；物业与车辆取提交时的主档快照；提交时间由服务端生成。
// 检查 FAIL 如实保存，不自动限制司机接单或状态（Q2-01 基线：先保存事实）。经 CreateWorkflow（幂等键 + 记录与审计同事务），
// 同键同内容重放得到同一份记录，不产生重复事实。
internal sealed class SubmitInspectionHandler(
    CreateWorkflow workflow,
    IDriverRepository drivers,
    IDriverInspectionRepository inspections,
    TimeProvider timeProvider) : ISubmitInspectionHandler
{
    private const string Operation = "driver-inspection.submit";
    private const string ResourceType = "driver-inspection";

    public async Task<OperationResult<DriverInspection>> HandleAsync(SubmitInspectionCommand command, CancellationToken cancellationToken)
    {
        var driver = await drivers.FindByIdAsync(command.DriverId, cancellationToken);
        if (driver is null)
        {
            return OperationResult<DriverInspection>.Failed(OperationOutcome.NotFound);
        }

        if (driver.Status != DriverStatus.Active)
        {
            return OperationResult<DriverInspection>.Rejected([new FieldIssue("driver", "Only an active driver can submit an inspection")]);
        }

        var template = ResolveTemplate(command);
        IReadOnlyList<FieldIssue> issues = template is null
            ? [new FieldIssue("templateVersion", "Unknown inspection template version")]
            : InspectionRules.Validate(template, command.Answers);

        return issues.Count > 0
            ? OperationResult<DriverInspection>.Invalid(issues)
            : await workflow.ExecuteAsync(BuildSpecification(command, driver, template!), cancellationToken);
    }

    private static InspectionTemplate? ResolveTemplate(SubmitInspectionCommand command) =>
        command.TemplateVersion is { } version
            ? InspectionTemplates.Find(command.Phase, version)
            : InspectionTemplates.CurrentFor(command.Phase);

    private CreateSpecification<DriverInspection> BuildSpecification(SubmitInspectionCommand command, Driver driver, InspectionTemplate template) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(DriverInspection.Create(
                driver.DriverId, driver.PropertyId, driver.AssignedVehicle, template, command.Answers, timeProvider.GetUtcNow())),
            inspection => inspection.InspectionId,
            (inspection, context, token) => inspections.InsertAsync(inspection, context, token),
            (id, token) => inspections.FindByIdAsync(id, token),
            (inspection, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, inspection.InspectionId, now, inspection.PropertyId));
}
