using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Application.Reports.Options;
using LMS.Reporting.Application.Reports.Ports;
using LMS.Reporting.Domain.Rows;
using Microsoft.Extensions.Options;

namespace LMS.Reporting.Application.Reports.Handlers;

// 报表是只读读取模型：来源未接入时返回“来源不可用”，不能把它伪装成已生成的空结果；
// 已接入但当前没有匹配的行则是真实的空列表。显式选择授权范围之外的物业返回 403。
internal sealed class ReportQueryHandler(
    IReportRowReader reader,
    IOptions<ReportingOptions> options) : IReportQueryHandler
{
    public async Task<OperationResult<PageResult<ReportRow>>> ListAsync(ReportRowsQuery query, CancellationToken cancellationToken)
    {
        if (!options.Value.RowsSourceConnected)
        {
            return OperationResult<PageResult<ReportRow>>.Failed(OperationOutcome.SourceUnavailable);
        }

        if (query.PropertyId is not null && !query.Scope.Allows(query.PropertyId))
        {
            return OperationResult<PageResult<ReportRow>>.Failed(OperationOutcome.Forbidden);
        }

        if (query.From is not null && query.To is not null && query.From >= query.To)
        {
            return OperationResult<PageResult<ReportRow>>.Invalid([new FieldIssue("to", "Must be later than from")]);
        }

        return OperationResult<PageResult<ReportRow>>.Succeeded(await reader.ListAsync(query, cancellationToken));
    }
}
