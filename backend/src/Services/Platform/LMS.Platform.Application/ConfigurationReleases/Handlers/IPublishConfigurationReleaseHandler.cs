using LMS.Platform.Application.ConfigurationReleases.Models;
using LMS.Platform.Application.ConfigurationReleases.Requests;

namespace LMS.Platform.Application.ConfigurationReleases.Handlers;

public interface IPublishConfigurationReleaseHandler
{
    Task<PublishConfigurationReleaseResult> HandleAsync(PublishConfigurationReleaseRequest request, CancellationToken cancellationToken);
}
