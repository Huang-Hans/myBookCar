using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Reporting.Application.Reports.Models;
using LMS.Reporting.Domain.Rows;

namespace LMS.Reporting.Api.Contracts.Reports;

// 五类报表预览共用的查询条件；各报表只在允许的排序字段与默认排序上不同。
public abstract class ReportListRequest : PagedQueryRequest
{
    [Description("按物业筛选；省略表示调用者有权访问的全部物业，不能借此获得全物业权限。传入时不能为空白。")]
    public string? PropertyId { get; set; }

    [Description("统计区间起点（含），ISO 8601 UTC 时间并以 Z 结尾；不传表示不限起点；只返回统计窗口落在 [from, to) 内的行。")]
    public DateTimeOffset? From { get; set; }

    [Description("统计区间终点（不含），ISO 8601 UTC 时间并以 Z 结尾；不传表示不限终点；两者都传时必须晚于 from。")]
    public DateTimeOffset? To { get; set; }

    [Description("按服务类型筛选：CROSS_BORDER、PROPERTY_TRANSFER；不传表示不区分。")]
    public ServiceType? ServiceType { get; set; }

    protected static ListQuerySpec SpecOf(ReportType type)
    {
        var sorting = ReportSorting.Of(type);

        return new ListQuerySpec(sorting.Fields, sorting.DefaultSort, ReportSorting.StableField);
    }
}
