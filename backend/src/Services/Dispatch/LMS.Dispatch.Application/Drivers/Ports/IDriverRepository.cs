using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Drivers.Models;
using LMS.Dispatch.Domain.Drivers;

namespace LMS.Dispatch.Application.Drivers.Ports;

public interface IDriverRepository
{
    Task<IReadOnlyList<Driver>> FindVisibleByIdsAsync(IReadOnlyList<string> driverIds,
        PropertyScope scope, ITransactionContext? context, CancellationToken cancellationToken);

    Task<Driver?> FindByIdAsync(string driverId, CancellationToken cancellationToken);

    // 只返回对调用者物业范围可见的司机；不可见与不存在不作区分。
    Task<Driver?> FindVisibleAsync(string driverId, PropertyScope scope, CancellationToken cancellationToken);

    Task<Driver?> FindVisibleAsync(string driverId, PropertyScope scope, ITransactionContext context, CancellationToken cancellationToken);

    Task<IReadOnlyList<Driver>> FindVisibleByIdsAsync(IReadOnlyList<string> driverIds, PropertyScope scope, CancellationToken cancellationToken);

    // 按标识取司机，不套用物业范围。只给已经通过范围检查的班次补姓名，不能当作列表接口。
    Task<IReadOnlyList<Driver>> FindByIdsAsync(IReadOnlyList<string> driverIds, CancellationToken cancellationToken);

    Task<PageResult<Driver>> ListAsync(ListDriversQuery query, CancellationToken cancellationToken);

    // 派单候选：筛选条件与 DispatchCandidateRules 一致，分页总数与结果使用同一筛选。
    Task<PageResult<Driver>> ListDispatchCandidatesAsync(DispatchCandidateQuery query, CancellationToken cancellationToken);

    // 执照号（不区分大小写）重复时抛出 DuplicateRecordException。
    Task InsertAsync(Driver driver, ITransactionContext context, CancellationToken cancellationToken);

    // 以期望版本为条件替换；版本已变化或记录不存在时返回 false。执照号重复时抛出 DuplicateRecordException。
    Task<bool> TryReplaceAsync(Driver updated, long expectedVersion, ITransactionContext context, CancellationToken cancellationToken);
}
