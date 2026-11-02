using LMS.BuildingBlocks.Core.Querying;
using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Locations.Models;
using LMS.Platform.Domain.Locations;

namespace LMS.Platform.Application.Locations.Ports;

public interface ILocationRepository
{
    Task<CommonLocation?> FindByIdAsync(string locationId, CancellationToken cancellationToken);

    Task<CommonLocation?> FindAsync(string locationId, ITransactionContext context, CancellationToken cancellationToken);

    Task<PageResult<CommonLocation>> ListAsync(ListLocationsQuery query, CancellationToken cancellationToken);

    // 地点名称（不区分大小写）重复时抛出 DuplicateRecordException。
    Task InsertAsync(CommonLocation location, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。名称重复时抛出 DuplicateRecordException。
    Task<bool> TryReplaceAsync(CommonLocation updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);

    // 该区域末尾的排序位置（现有最大值加一，区域为空时为 1）。
    Task<int> NextSortOrderAsync(LocationRegion region, ITransactionContext context, CancellationToken cancellationToken);

    Task<IReadOnlyList<CommonLocation>> ListByRegionAsync(LocationRegion region, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件设置排序位置并递增版本。
    Task<bool> TrySetSortOrderAsync(
        string locationId,
        long expectedVersion,
        int sortOrder,
        ITransactionContext context,
        CancellationToken cancellationToken);
}
