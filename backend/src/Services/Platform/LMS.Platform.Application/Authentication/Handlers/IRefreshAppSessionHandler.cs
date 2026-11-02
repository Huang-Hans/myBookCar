using LMS.Platform.Application.Authentication.Models;

namespace LMS.Platform.Application.Authentication.Handlers;

public interface IRefreshAppSessionHandler
{
    Task<AppAuthResult> HandleAsync(AppRefreshCommand command, CancellationToken cancellationToken);
}
