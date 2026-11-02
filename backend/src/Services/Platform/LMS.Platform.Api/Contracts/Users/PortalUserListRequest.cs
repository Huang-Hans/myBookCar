using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.Users.Models;

namespace LMS.Platform.Api.Contracts.Users;

public sealed class PortalUserListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = new(PortalUserSorting.Fields, PortalUserSorting.DefaultSort, PortalUserSorting.StableField);

    [Description("按物业筛选；传入时不能为空白，且须在调用者物业范围内。")]
    public string? PropertyId { get; set; }

    [Description("按启用状态筛选；不传时不过滤。")]
    public bool? IsActive { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
