using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Configuration.Catalogs;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Application.Locations.Ports;
using LMS.Platform.Application.Workflows;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

internal sealed class UpdateLocationHandler(
    PatchWorkflow workflow,
    ILocationRepository locations,
    ILocationCategoryCatalog categories) : IUpdateLocationHandler
{
    private const string Operation = "location.update";
    private const string ResourceType = "location";

    public async Task<OperationResult<CommonLocation>> HandleAsync(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var categoryCode = command.Changes.CategoryCode;
        var rejection = categoryCode.IsSet && categoryCode.Value is not null
            ? PropertyReferenceCheck.ToFailure<CommonLocation>(
                await categories.ContainsAsync(categoryCode.Value, cancellationToken), "categoryCode", "Must be an uploaded category code")
            : null;

        return rejection ?? await workflow.ExecuteAsync(BuildSpecification(command), cancellationToken);
    }

    private PatchSpecification<CommonLocation> BuildSpecification(UpdateLocationCommand command) =>
        new(
            command.LocationId,
            command.ExpectedVersion,
            (id, context, token) => locations.FindAsync(id, context, token),
            location => location.Version,
            (location, context, token) => ApplyAsync(location, command, context, token),
            (updated, expected, context, token) => locations.TryReplaceAsync(updated, expected, context, token),
            (_, after, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, after.LocationId, now, command.Changes.ChangedFields(), null));

    // 合并后校验完整资源；改变区域的地点放到新区域末尾，避免与新区域已有地点的排序位置冲突。
    private async Task<OperationResult<CommonLocation>> ApplyAsync(
        CommonLocation current,
        UpdateLocationCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var merged = current.Apply(command.Changes);
        var issues = LocationRules.Validate(merged);
        if (issues.Count > 0)
        {
            return OperationResult<CommonLocation>.Invalid(issues);
        }

        if (merged.Region == current.Region)
        {
            return OperationResult<CommonLocation>.Succeeded(merged);
        }

        var sortOrder = await locations.NextSortOrderAsync(merged.Region, context, cancellationToken);
        return OperationResult<CommonLocation>.Succeeded(merged with { SortOrder = sortOrder });
    }
}
