using LMS.BuildingBlocks.Http.Requests;
using LMS.BuildingBlocks.Http.Requests.Contracts;
using LMS.Platform.Application.Configuration.Definitions;

namespace LMS.Platform.Api.Contracts.Configuration;

public sealed class TravelBufferListRequest : PagedQueryRequest
{
    private static readonly ListQuerySpec Spec = ConfigurationListSpecs.Of(TravelBufferDefinition.Create());

    public override ListQuerySpec GetSortSpecification() => Spec;
}
