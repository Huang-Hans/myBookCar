using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Http.Requests.Validation;

namespace LMS.BuildingBlocks.Http.Requests.Contracts;

public abstract class PagedQueryRequest : QueryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Must be a positive integer")]
    [Description("页码，从 1 开始。")]
    public int Page { get; set; } = PageSizes.DefaultPage;

    [PageSize]
    [Description("每页条数。")]
    public int PageSize { get; set; } = PageSizes.DefaultPageSize;

    [UnrestrictedText]
    [Description("排序字段，多个字段以逗号分隔；字段前加 '-' 表示降序。")]
    public string? Sort { get; set; }

    [UnrestrictedText]
    [Description("在可见字段中进行不区分大小写的搜索；空白表示不筛选。")]
    public string Search { get; set; } = string.Empty;

    internal RequestParseResult<ListQuery>? BindingResult { get; set; }

    public abstract ListQuerySpec GetSortSpecification();

    public ListQuery ToListQuery() => BindingResult?.Value
        ?? throw new InvalidOperationException("The query request has not been bound.");
}
