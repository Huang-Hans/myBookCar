using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;

namespace LMS.Platform.Application.Configuration.Handlers;

public interface IConfigurationListHandler<TItem>
{
    // filter 由接入层按已校验的查询参数与授权范围构造，在搜索和分页之前应用。
    Task<OperationResult<PageResult<TItem>>> ListAsync(ListQuery query, Func<TItem, bool>? filter, CancellationToken cancellationToken);

    // 不分页的卡片或矩阵：按来源顺序返回全部满足 filter 的条目。
    Task<OperationResult<IReadOnlyList<TItem>>> ListAllAsync(Func<TItem, bool>? filter, CancellationToken cancellationToken);

    // 按稳定代码读取一条；不存在或被 filter 排除时为 NotFound。
    Task<OperationResult<TItem>> FindAsync(string key, Func<TItem, bool>? filter, CancellationToken cancellationToken);
}
