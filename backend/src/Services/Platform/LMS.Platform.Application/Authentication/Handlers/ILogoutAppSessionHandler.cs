using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Application.Authentication.Handlers;

public interface ILogoutAppSessionHandler
{
    Task<AppAuthResult> HandleAsync(AppLogoutCommand command, CancellationToken cancellationToken);
}
