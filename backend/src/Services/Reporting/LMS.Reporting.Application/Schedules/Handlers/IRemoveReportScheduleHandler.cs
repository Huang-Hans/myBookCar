using LMS.BuildingBlocks.Core.Operations;
using LMS.Reporting.Application.Schedules.Commands;

namespace LMS.Reporting.Application.Schedules.Handlers;

public interface IRemoveReportScheduleHandler
{
    // 移除计划并保留审计。计划已不存在（或对调用者不可见）时返回相同的成功，Value 表示本次是否真的删除了记录。
    Task<OperationResult<bool>> HandleAsync(RemoveReportScheduleCommand command, CancellationToken cancellationToken);
}
