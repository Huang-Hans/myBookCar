using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.Configuration.Definitions;

namespace LMS.Platform.Api.Contracts.Configuration;

public sealed class PropertyListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = ConfigurationListSpecs.Of(PropertyDefinition.Create());

    public PropertyStatus? Status { get; init; }

    public override ListQuerySpec GetSortSpecification() => Spec;
}
