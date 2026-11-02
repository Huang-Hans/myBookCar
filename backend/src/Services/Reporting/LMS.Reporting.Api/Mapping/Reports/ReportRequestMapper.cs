using LMS.BuildingBlocks.Core.Scoping;
using LMS.Reporting.Api.Contracts.Reports;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Mapping.Reports;

internal static class ReportRequestMapper
{
    // From / To 都是可选的；两者都给出时 to 晚于 from 由用例校验。
    public static ReportRowsQuery ToQuery(ReportType type, ReportListRequest request, PropertyScope scope) =>
        new(type, request.ToListQuery(), scope, request.PropertyId, request.From, request.To, request.ServiceType);
}
