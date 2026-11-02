using LMS.Dispatch.Application.Credentials.Ports;

namespace LMS.Dispatch.Application.Credentials.Handlers;

internal sealed class CheckDriverLoginEligibilityHandler(IDriverCredentialReader credentials) : ICheckDriverLoginEligibilityHandler
{
    public async Task<bool> HandleAsync(string driverId, CancellationToken cancellationToken)
    {
        var credential = await credentials.FindByIdAsync(driverId, cancellationToken);

        return credential is not null && DriverLoginRules.IsLoginAllowed(credential);
    }
}
