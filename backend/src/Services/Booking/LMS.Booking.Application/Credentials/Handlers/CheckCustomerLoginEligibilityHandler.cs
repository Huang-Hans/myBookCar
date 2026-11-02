using LMS.Booking.Application.Credentials.Ports;

namespace LMS.Booking.Application.Credentials.Handlers;

internal sealed class CheckCustomerLoginEligibilityHandler(ICustomerCredentialReader credentials) : ICheckCustomerLoginEligibilityHandler
{
    public async Task<bool> HandleAsync(string guestId, CancellationToken cancellationToken)
    {
        var credential = await credentials.FindByIdAsync(guestId, cancellationToken);

        return credential is not null && !string.IsNullOrEmpty(credential.PasswordHash);
    }
}
