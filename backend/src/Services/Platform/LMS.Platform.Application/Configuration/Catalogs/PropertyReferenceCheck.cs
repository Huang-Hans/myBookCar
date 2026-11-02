using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Platform.Application.Configuration.Catalogs;

internal static class PropertyReferenceCheck
{
    // 返回 null 表示引用有效（或没有引用）；否则返回应直接给出的失败结果。
    public static async Task<OperationResult<T>?> ResolveAsync<T>(
        IPropertyCatalog catalog,
        string? propertyId,
        CancellationToken cancellationToken)
    {
        if (propertyId is null)
        {
            return null;
        }

        return ToFailure<T>(await catalog.ContainsAsync(propertyId, cancellationToken), "propertyId", "Must be an uploaded property code");
    }

    public static OperationResult<T>? ToFailure<T>(CatalogLookup lookup, string field, string message) =>
        lookup switch
        {
            CatalogLookup.Found => null,
            CatalogLookup.NotFound => OperationResult<T>.Invalid([new FieldIssue(field, message)]),
            _ => OperationResult<T>.Failed(OperationOutcome.SourceUnavailable)
        };
}
