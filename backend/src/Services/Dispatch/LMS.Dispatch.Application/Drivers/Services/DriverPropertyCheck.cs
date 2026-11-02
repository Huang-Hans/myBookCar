using LMS.BuildingBlocks.Core.Operations;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Dispatch.Application.Directory;

namespace LMS.Dispatch.Application.Drivers.Services;

internal static class DriverPropertyCheck
{
    // 目标物业必须在调用者授权范围内，并且是上传配置中的物业代码。返回 null 表示通过。
    public static async Task<OperationResult<T>?> CheckAsync<T>(
        IPlatformDirectory directory,
        PropertyScope scope,
        string propertyId,
        CancellationToken cancellationToken)
    {
        if (!scope.Allows(propertyId))
        {
            return OperationResult<T>.Failed(OperationOutcome.Forbidden);
        }

        return await directory.ContainsPropertyAsync(propertyId, cancellationToken) switch
        {
            PropertyLookup.Found => null,
            PropertyLookup.NotFound => OperationResult<T>.Invalid([new FieldIssue("propertyId", "Must be an uploaded property code")]),
            _ => OperationResult<T>.Failed(OperationOutcome.SourceUnavailable)
        };
    }
}
