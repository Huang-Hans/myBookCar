using LMS.BuildingBlocks.Core.Authorization;
using LMS.Booking.Application.Directory;
using LMS.Booking.Infrastructure.ServiceIdentity;

namespace LMS.Booking.Infrastructure.Remote;

internal sealed class DriverContactDirectory(HttpClient client, IServiceTokenIssuer tokens) : IDriverContactDirectory
{
    public async Task<DirectoryResult<string>> GetPhoneNumberAsync(string driverId, CancellationToken cancellationToken)
    {
        if (!tokens.IsAvailable)
        {
            return DirectoryResult<string>.Failed(DirectoryOutcome.Unavailable);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/v1/drivers/{Uri.EscapeDataString(driverId)}/contacts");
        var response = await RemoteHttp.SendAsync(client, request, tokens.Issue(ServiceScopes.DriverContactRead), cancellationToken);
        return await RemoteHttp.ReadAsync<RemoteDriverContactPayload, string>(response, contact => contact.PhoneNumber, cancellationToken);
    }

    private sealed record RemoteDriverContactPayload(string DriverId, string PhoneNumber);
}
