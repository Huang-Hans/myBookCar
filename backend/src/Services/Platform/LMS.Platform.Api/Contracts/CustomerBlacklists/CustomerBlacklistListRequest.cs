using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.CustomerBlacklists.Models;

namespace LMS.Platform.Api.Contracts.CustomerBlacklists;

public sealed class CustomerBlacklistListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec =
        new(CustomerBlacklistSorting.Fields, CustomerBlacklistSorting.DefaultSort, CustomerBlacklistSorting.StableField);

    [Description("按记录状态筛选（不是日期有效性）；不传时不过滤。")]
    public bool? IsActive { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
