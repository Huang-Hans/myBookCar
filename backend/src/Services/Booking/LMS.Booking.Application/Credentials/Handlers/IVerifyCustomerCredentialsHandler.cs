using LMS.Booking.Application.Credentials.Models;

namespace LMS.Booking.Application.Credentials.Handlers;

public interface IVerifyCustomerCredentialsHandler
{
    Task<CustomerCredentialVerification> HandleAsync(string phone, string password, CancellationToken cancellationToken);
}
