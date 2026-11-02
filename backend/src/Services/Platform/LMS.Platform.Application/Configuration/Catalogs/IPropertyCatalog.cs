namespace LMS.Platform.Application.Configuration.Catalogs;

public interface IPropertyCatalog
{
    Task<CatalogLookup> ContainsAsync(string propertyId, CancellationToken cancellationToken);
}
