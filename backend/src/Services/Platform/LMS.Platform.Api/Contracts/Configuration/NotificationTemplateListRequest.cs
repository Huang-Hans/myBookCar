using System.ComponentModel;
using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.Configuration.Definitions;

namespace LMS.Platform.Api.Contracts.Configuration;

public sealed class NotificationTemplateListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = ConfigurationListSpecs.Of(NotificationTemplateDefinition.Create());

    [Description("按启用状态筛选；不传时不过滤。")]
    public bool? IsActive { get; set; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
