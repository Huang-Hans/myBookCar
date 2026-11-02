using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Auditing.Ports;
using LMS.Platform.Application.Auditing.Services;
using LMS.Platform.Application.Locations.Commands;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Application.Locations.Ports;
using LMS.Platform.Domain.Auditing;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Handlers;

// 替换区域内顺序：请求必须恰好覆盖该区域的全部地点；乐观并发暂时取消，不比较条目里的版本。
// 读取与写入在同一事务内完成；事务内的并发修改会触发重试并重新校验，因此不会用过期的成员集合落库。
internal sealed class ReplaceLocationOrderHandler(
    ITransactionRunner transactionRunner,
    ILocationRepository locations,
    IAuditRecordRepository auditRecords,
    TimeProvider timeProvider) : IReplaceLocationOrderHandler
{
    private const string Operation = "location.reorder";
    private const string ResourceType = "location-ordering";

    public Task<OperationResult<LocationOrdering>> HandleAsync(ReplaceLocationOrderCommand command, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteAsync((context, token) => ReplaceAsync(command, context, token), cancellationToken);

    private async Task<OperationResult<LocationOrdering>> ReplaceAsync(
        ReplaceLocationOrderCommand command,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var members = await locations.ListByRegionAsync(command.Region, context, cancellationToken);
        var rejection = Verify(command, members);
        if (rejection is not null)
        {
            return rejection;
        }

        var results = await WriteOrderAsync(command, members, context, cancellationToken);
        await auditRecords.InsertAsync(CreateAudit(command), context, cancellationToken);

        return OperationResult<LocationOrdering>.Succeeded(new LocationOrdering(command.Region, results));
    }

    private static OperationResult<LocationOrdering>? Verify(ReplaceLocationOrderCommand command, IReadOnlyList<CommonLocation> members)
    {
        var requestedIds = command.Items.Select(item => item.LocationId).ToHashSet(StringComparer.Ordinal);
        if (!requestedIds.SetEquals(members.Select(member => member.LocationId)))
        {
            return OperationResult<LocationOrdering>.Invalid(
                [new FieldIssue("items", "Must contain every location of the region exactly once")]);
        }

        return null;
    }

    private async Task<IReadOnlyList<LocationOrderResult>> WriteOrderAsync(
        ReplaceLocationOrderCommand command,
        IReadOnlyList<CommonLocation> members,
        ITransactionContext context,
        CancellationToken cancellationToken)
    {
        var versions = members.ToDictionary(member => member.LocationId, member => member.Version, StringComparer.Ordinal);
        var results = new List<LocationOrderResult>(command.Items.Count);
        for (var index = 0; index < command.Items.Count; index++)
        {
            var item = command.Items[index];
            var sortOrder = index + 1;
            var written = await locations.TrySetSortOrderAsync(item.LocationId, versions[item.LocationId], sortOrder, context, cancellationToken);
            results.Add(written
                ? new LocationOrderResult(item.LocationId, sortOrder, versions[item.LocationId] + 1)
                : throw new InvalidOperationException("A location changed inside the ordering transaction after its version was verified."));
        }

        return results;
    }

    private AuditRecord CreateAudit(ReplaceLocationOrderCommand command) =>
        AuditRecordFactory.Succeeded(
            command.Context,
            Operation,
            ResourceType,
            WireEnum.ToWire(command.Region),
            timeProvider.GetUtcNow(),
            $"ordered {command.Items.Count} locations",
            null);
}
