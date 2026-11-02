namespace LMS.Platform.Application.Configuration.Catalogs;

public interface ILocationCategoryCatalog
{
    Task<CatalogLookup> ContainsAsync(string categoryCode, CancellationToken cancellationToken);
}
