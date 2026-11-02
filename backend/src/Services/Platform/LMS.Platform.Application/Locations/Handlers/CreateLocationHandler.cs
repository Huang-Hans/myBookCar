using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Catalogs;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Application.Locations.Ports;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

// 新地点放在所属区域的末尾；排序位置在事务内读取，避免与并发新增取到同一个位置。
internal sealed class CreateLocationHandler(
    CreateWorkflow workflow,
    ILocationRepository locations,
    ILocationCategoryCatalog categories) : ICreateLocationHandler
{
    private const string Operation = "location.create";
    private const string ResourceType = "location";
    private const int UnassignedSortOrder = 0;

    public async Task<OperationResult<CommonLocation>> HandleAsync(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        var candidate = ToLocation(command);
        var issues = LocationRules.Validate(candidate);
        if (issues.Count > 0)
        {
            return OperationResult<CommonLocation>.Invalid(issues);
        }

        var categoryCheck = PropertyReferenceCheck.ToFailure<CommonLocation>(
            await categories.ContainsAsync(command.CategoryCode, cancellationToken), "categoryCode", "Must be an uploaded category code");

        return categoryCheck ?? await workflow.ExecuteAsync(BuildSpecification(command, candidate), cancellationToken);
    }

    private CreateSpecification<CommonLocation> BuildSpecification(CreateLocationCommand command, CommonLocation candidate) =>
        new(
            IdempotencyScopes.For(command.Context, Operation, command.IdempotencyKey),
            command.ToCanonicalForm(),
            async (context, token) => candidate with { SortOrder = await locations.NextSortOrderAsync(candidate.Region, context, token) },
            location => location.LocationId,
            (location, context, token) => locations.InsertAsync(location, context, token),
            (id, token) => locations.FindByIdAsync(id, token),
            (location, now) => AuditRecordFactory.Created(command.Context, Operation, ResourceType, location.LocationId, now, null));

    private static CommonLocation ToLocation(CreateLocationCommand command) =>
        CommonLocation.Create(
            command.Name, command.Address, command.Coordinates, command.Region, command.CategoryCode, command.ContactPerson,
            command.ContactPhone, command.OperatingHours, command.Geofence, command.IsActive, command.Notes, UnassignedSortOrder);
}
