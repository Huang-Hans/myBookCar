using LMS.Platform.Application.Abstractions;
using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Ports;

public interface IPropertyRepository
{
    Task<ConfiguredProperty?> FindAsync(string propertyId, CancellationToken cancellationToken);
    Task<ConfiguredProperty?> FindAsync(string propertyId, ITransactionContext context, CancellationToken cancellationToken);
    Task InsertAsync(ConfiguredProperty property, string actor, ITransactionContext context, CancellationToken cancellationToken);
    Task<bool> ReplaceAsync(ConfiguredProperty property, string actor, ITransactionContext context, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(ConfiguredProperty property, string actor, ITransactionContext context, CancellationToken cancellationToken);
}
