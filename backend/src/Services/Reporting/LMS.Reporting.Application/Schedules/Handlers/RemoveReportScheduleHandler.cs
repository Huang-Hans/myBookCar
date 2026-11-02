using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Abstractions;
using LMS.Reporting.Application.Auditing.Ports;
using LMS.Reporting.Application.Auditing.Services;
using LMS.Reporting.Application.Schedules.Commands;
using LMS.Reporting.Application.Schedules.Ports;
using LMS.Reporting.Domain.Auditing;
using LMS.Reporting.Domain.Schedules;

namespace LMS.Reporting.Application.Schedules.Handlers;

// 移除报告计划：计划记录与对应的审计在同一事务提交。对不可见的计划不删除，响应与“已不存在”相同，不泄露它是否存在。
internal sealed class RemoveReportScheduleHandler(
    ITransactionRunner transactions,
    IReportScheduleRepository schedules,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider) : IRemoveReportScheduleHandler
{
    private const string Operation = "report-schedule.remove";
    private const string ResourceType = "report-schedule";
    private const string RemovedSummary = "removed";

    public async Task<OperationResult<bool>> HandleAsync(RemoveReportScheduleCommand command, CancellationToken cancellationToken)
    {
        var removed = await transactions.ExecuteAsync((context, token) => RemoveAsync(command, context, token), cancellationToken);

        return OperationResult<bool>.Succeeded(removed);
    }

    private async Task<bool> RemoveAsync(RemoveReportScheduleCommand command, ITransactionContext context, CancellationToken cancellationToken)
    {
        var schedule = await schedules.FindAsync(command.ScheduleId, context, cancellationToken);
        if (schedule is null || !schedule.IsVisibleTo(command.Context.Scope) || !await schedules.DeleteAsync(schedule.ScheduleId, context, cancellationToken))
        {
            return false;
        }

        await auditRecords.InsertAsync(BuildAudit(command, schedule), context, cancellationToken);

        return true;
    }

    private AuditRecord BuildAudit(RemoveReportScheduleCommand command, ReportSchedule schedule) =>
        AuditRecordFactory.Succeeded(
            command.Context, Operation, ResourceType, schedule.ScheduleId, timeProvider.GetUtcNow(), RemovedSummary, schedule.Scope.PropertyId);
}
