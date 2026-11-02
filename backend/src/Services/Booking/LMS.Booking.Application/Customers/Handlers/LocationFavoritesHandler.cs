using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Auditing.Services;
using LMS.Booking.Application.Catalog.Ports;
using LMS.Booking.Application.Customers.Commands;
using LMS.Booking.Application.Customers.Models;
using LMS.Booking.Application.Customers.Options;
using LMS.Booking.Application.Customers.Ports;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.SelfService.Ports;
using LMS.Booking.Application.Workflows;
using LMS.Booking.Domain.Customers;
using Microsoft.Extensions.Options;

namespace LMS.Booking.Application.Customers.Handlers;

// 上车 / 下车两组收藏。收藏的是目录地点引用加保存时的名称、区域快照：自由文本不能冒充地点标识，也不创建公共地点
// （自由地址的受约束解析尚未提供，是否允许保存不可订文本待 Q2-05 确认，因此本版只接受目录地点）。
// 保存即整体替换该分组；地点之后停用或更名，收藏保留原引用，读取时给出可用性，不静默删除。
// 收藏存在 customer_location_favorites，与员工偏好（staff_preferences）完全分开。
internal sealed class LocationFavoritesHandler(
    UpsertWorkflow workflow,
    IPlatformSelfCatalog catalog,
    ICustomerLocationFavoritesRepository favorites,
    IOptions<FavoritesOptions> options) : ILocationFavoritesHandler
{
    private const string Operation = "customer-favorites.replace";
    private const string ResourceType = "customer-favorites";

    public async Task<OperationResult<FavoritesResult>> GetAsync(string guestId, FavoriteUsage usage, CancellationToken cancellationToken)
    {
        var stored = await favorites.FindAsync(guestId, usage, cancellationToken) ?? CustomerLocationFavorites.Empty(guestId, usage);

        return OperationResult<FavoritesResult>.Succeeded(new FavoritesResult(stored, await WithAvailabilityAsync(stored, cancellationToken)));
    }

    public async Task<OperationResult<FavoritesResult>> ReplaceAsync(ReplaceLocationFavoritesCommand command, CancellationToken cancellationToken)
    {
        var issues = CheckShape(command);
        if (issues.Count > 0)
        {
            return OperationResult<FavoritesResult>.Invalid(issues);
        }

        var resolved = await ResolveAsync(command.LocationIds, cancellationToken);
        if (!resolved.IsSuccess)
        {
            return OperationResult<FavoritesResult>.FailureOf(resolved);
        }

        var saved = await workflow.ExecuteAsync(BuildSpecification(command, resolved.Value!), cancellationToken);
        if (!saved.IsSuccess)
        {
            return OperationResult<FavoritesResult>.FailureOf(saved);
        }

        return OperationResult<FavoritesResult>.Succeeded(new FavoritesResult(saved.Value!, await WithAvailabilityAsync(saved.Value!, cancellationToken)));
    }

    private IReadOnlyList<FieldIssue> CheckShape(ReplaceLocationFavoritesCommand command)
    {
        var issues = new List<FieldIssue>();
        var max = options.Value.MaxItemsPerUsage;
        if (command.LocationIds.Count > max)
        {
            issues.Add(new FieldIssue("items", $"Must not contain more than {max} locations"));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < command.LocationIds.Count; index++)
        {
            if (!seen.Add(command.LocationIds[index]))
            {
                issues.Add(new FieldIssue($"items[{index}].locationId", "Duplicate location in the same group"));
            }
        }

        return issues;
    }

    // 保存时逐个核对目录：必须存在且启用；目录不可用不能给出真实结果，按来源不可用处理。
    private async Task<OperationResult<IReadOnlyList<LocationFavorite>>> ResolveAsync(
        IReadOnlyList<string> locationIds,
        CancellationToken cancellationToken)
    {
        var lookups = await Task.WhenAll(locationIds.Select(id => catalog.GetLocationAsync(id, cancellationToken)));
        var items = new List<LocationFavorite>();
        for (var index = 0; index < lookups.Length; index++)
        {
            var failure = FailureOf(lookups[index], index);
            if (failure is not null)
            {
                return failure;
            }

            var location = lookups[index].Value!;
            items.Add(new LocationFavorite(location.LocationId, location.Name, location.Region));
        }

        return OperationResult<IReadOnlyList<LocationFavorite>>.Succeeded(items);
    }

    private static OperationResult<IReadOnlyList<LocationFavorite>>? FailureOf(DirectoryResult<CatalogLocation> lookup, int index)
    {
        var field = $"items[{index}].locationId";

        return lookup.Outcome switch
        {
            DirectoryOutcome.Success when lookup.Value!.IsActive => null,
            DirectoryOutcome.Success => OperationResult<IReadOnlyList<LocationFavorite>>.Invalid([new FieldIssue(field, "Location is not active")]),
            DirectoryOutcome.NotFound => OperationResult<IReadOnlyList<LocationFavorite>>.Invalid([new FieldIssue(field, "Unknown location")]),
            DirectoryOutcome.Forbidden => OperationResult<IReadOnlyList<LocationFavorite>>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<IReadOnlyList<LocationFavorite>>.Failed(OperationOutcome.SourceUnavailable)
        };
    }

    private UpsertSpecification<CustomerLocationFavorites> BuildSpecification(
        ReplaceLocationFavoritesCommand command,
        IReadOnlyList<LocationFavorite> items) =>
        new(
            command.ExpectedVersion,
            CustomerLocationFavorites.Empty(command.GuestId, command.Usage),
            (context, token) => favorites.FindAsync(command.GuestId, command.Usage, context, token),
            record => record.Version,
            (record, version) => record with { Version = version },
            (record, now) => record.Replace(items, now),
            (before, after) => before.Items.SequenceEqual(after.Items),
            (record, context, token) => favorites.InsertAsync(record, context, token),
            (record, expected, context, token) => favorites.TryReplaceAsync(record, expected, context, token),
            (record, now) => AuditRecordFactory.Updated(
                command.Context, Operation, ResourceType, $"{record.GuestId}:{WireEnum.ToWire(record.Usage)}", now, ["items"], null));

    // 读取时逐项核对当前可用性；目录不可用只让该项的可用性未知，不让整个读取失败，也不删除任何收藏。
    private async Task<IReadOnlyList<FavoriteLocationItem>> WithAvailabilityAsync(
        CustomerLocationFavorites stored,
        CancellationToken cancellationToken)
    {
        var lookups = await Task.WhenAll(stored.Items.Select(item => catalog.GetLocationAsync(item.LocationId, cancellationToken)));

        return stored.Items.Select((item, index) => new FavoriteLocationItem(item, AvailabilityOf(lookups[index]))).ToList();
    }

    private static bool? AvailabilityOf(DirectoryResult<CatalogLocation> lookup) => lookup.Outcome switch
    {
        DirectoryOutcome.Success => lookup.Value!.IsActive,
        DirectoryOutcome.NotFound => false,
        _ => null
    };
}
