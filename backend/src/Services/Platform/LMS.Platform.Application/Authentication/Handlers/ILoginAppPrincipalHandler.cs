using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Application.Authentication.Handlers;

public interface ILoginAppPrincipalHandler
{
    Task<AppAuthResult> HandleAsync(AppLoginCommand command, CancellationToken cancellationToken);
}
