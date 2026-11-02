using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Catalogs;

internal sealed class LocationCategoryCatalog(IConfigurationListHandler<LocationCategory> categories) : ILocationCategoryCatalog
{
    public async Task<CatalogLookup> ContainsAsync(string categoryCode, CancellationToken cancellationToken)
    {
        var result = await categories.FindAsync(categoryCode, null, cancellationToken);

        return CatalogLookups.From(result.Outcome);
    }
}
