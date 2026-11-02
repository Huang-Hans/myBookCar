using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Querying;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.SelfService.Ports;

namespace LMS.Booking.Application.SelfService.Handlers;

// 客户的地点搜索：只读代理 Platform 地点目录中启用的地点，不创建任何公共地点，也不接受任意文本冒充地点 ID。
internal sealed class CustomerLocationsHandler(IPlatformSelfCatalog catalog) : ICustomerLocationsHandler
{
    public async Task<OperationResult<PageResult<CatalogLocationDetail>>> ListAsync(
        string? region,
        ListQuery list,
        CancellationToken cancellationToken)
    {
        var result = await catalog.ListLocationsAsync(list.HasSearch ? list.Search : null, region, list.Page, list.PageSize, cancellationToken);

        return result.Outcome switch
        {
            DirectoryOutcome.Success => OperationResult<PageResult<CatalogLocationDetail>>.Succeeded(
                new PageResult<CatalogLocationDetail>(result.Value!.Items, result.Value.TotalItems)),
            DirectoryOutcome.Forbidden => OperationResult<PageResult<CatalogLocationDetail>>.Failed(OperationOutcome.Forbidden),
            DirectoryOutcome.Rejected => OperationResult<PageResult<CatalogLocationDetail>>.Invalid(result.Issues),
            _ => OperationResult<PageResult<CatalogLocationDetail>>.Failed(OperationOutcome.SourceUnavailable)
        };
    }

    // 已停用的地点与不存在的地点同样返回 NotFound：客户只能看到预订流程可选的（启用的）目录地点，也不会为停用地点返回坐标。
    public async Task<OperationResult<CatalogLocationProfile>> GetAsync(string locationId, CancellationToken cancellationToken)
    {
        var result = await catalog.GetLocationProfileAsync(locationId, cancellationToken);

        return result.Outcome switch
        {
            DirectoryOutcome.Success when result.Value!.IsActive => OperationResult<CatalogLocationProfile>.Succeeded(result.Value),
            DirectoryOutcome.Success or DirectoryOutcome.NotFound => OperationResult<CatalogLocationProfile>.Failed(OperationOutcome.NotFound),
            DirectoryOutcome.Forbidden => OperationResult<CatalogLocationProfile>.Failed(OperationOutcome.Forbidden),
            _ => OperationResult<CatalogLocationProfile>.Failed(OperationOutcome.SourceUnavailable)
        };
    }
}
