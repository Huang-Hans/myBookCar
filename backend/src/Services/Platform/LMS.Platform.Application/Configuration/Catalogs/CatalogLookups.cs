using LMS.BuildingBlocks.Core.Operations;

namespace LMS.Platform.Application.Configuration.Catalogs;

internal static class CatalogLookups
{
    public static CatalogLookup From(OperationOutcome outcome) =>
        outcome switch
        {
            OperationOutcome.Succeeded => CatalogLookup.Found,
            OperationOutcome.NotFound => CatalogLookup.NotFound,
            _ => CatalogLookup.SourceUnavailable
        };
}
