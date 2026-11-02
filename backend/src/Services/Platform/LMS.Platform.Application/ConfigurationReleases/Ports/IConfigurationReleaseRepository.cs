using LMS.Platform.Application.Abstractions;
using LMS.Platform.Domain.Configuration;

namespace LMS.Platform.Application.ConfigurationReleases.Ports;

public interface IConfigurationReleaseRepository
{
    Task<ConfigurationRelease?> FindAsync(string releaseId, CancellationToken cancellationToken);

    Task InsertAsync(ConfigurationRelease release, ITransactionContext context, CancellationToken cancellationToken);
}
