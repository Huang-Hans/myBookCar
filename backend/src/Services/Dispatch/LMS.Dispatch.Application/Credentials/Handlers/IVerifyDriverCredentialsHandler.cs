using LMS.Dispatch.Application.Credentials.Models;

namespace LMS.Dispatch.Application.Credentials.Handlers;

public interface IVerifyDriverCredentialsHandler
{
    Task<DriverCredentialVerification> HandleAsync(string phone, string password, CancellationToken cancellationToken);
}
