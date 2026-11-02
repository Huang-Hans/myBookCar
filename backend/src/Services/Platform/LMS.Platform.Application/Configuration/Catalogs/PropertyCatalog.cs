using LMS.BuildingBlocks.Core.Operations;
using LMS.Platform.Application.Configuration.Handlers;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Catalogs;

internal sealed class PropertyCatalog(IConfigurationListHandler<ConfiguredProperty> properties) : IPropertyCatalog
{
    public async Task<CatalogLookup> ContainsAsync(string propertyId, CancellationToken cancellationToken)
    {
        var result = await properties.FindAsync(propertyId, item => item.IsActive && !item.IsDeleted, cancellationToken);

        return CatalogLookups.From(result.Outcome);
    }
}
