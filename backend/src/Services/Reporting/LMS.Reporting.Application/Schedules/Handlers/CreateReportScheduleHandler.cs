using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Auditing.Services;
using LMS.Reporting.Application.Schedules.Commands;
using LMS.Reporting.Application.Schedules.Ports;
using LMS.Reporting.Application.Workflows;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Application.Schedules.Handlers;

// 登记邮件报告计划：只登记，不发送。登记范围取自调用者的授权快照；同一范围内邮箱+频率+报表类型重复返回 409。
internal sealed class CreateReportScheduleHandler(
    CreateWorkflow workflow,
    IReportScheduleRepository schedules,
    TimeProvider timeProvider) : ICreateReportScheduleHandler
{
    private const string Operation = "report-schedule.create";
    private const string ResourceType = "report-schedule";

    public async Task<OperationResult<ReportSchedule>> HandleAsync(CreateReportScheduleCommand command, CancellationToken cancellationToken) =>
        await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);

    private CreateSpecification<ReportSchedule> BuildSpecification(CreateReportScheduleCommand command) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            (_, _) => Task.FromResult(ReportSchedule.Create(
                command.RecipientEmail, command.Frequency, command.ReportType, command.Context.Actor, command.Context.Scope, timeProvider.GetUtcNow())),
            schedule => schedule.ScheduleId,
            (schedule, context, token) => schedules.InsertAsync(schedule, context, token),
            (id, token) => schedules.FindAsync(id, token),
            (schedule, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, schedule.ScheduleId, now, command.Context.Scope.PropertyId));
}
