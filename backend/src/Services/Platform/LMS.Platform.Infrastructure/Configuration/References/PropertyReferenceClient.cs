using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LMS.Platform.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace LMS.Platform.Infrastructure.Configuration.References;

internal sealed class PropertyReferenceClient(
    HttpClient client, ICallerToken callerToken, ILogger<PropertyReferenceClient> logger)
{
    public async Task<bool?> HasReferencesAsync(string baseAddress, string propertyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(callerToken.Value)) return null;
        var path = $"internal/v1/properties/{Uri.EscapeDataString(propertyId)}/references";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(baseAddress.TrimEnd('/') + "/"), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", callerToken.Value);
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;
            var envelope = await response.Content.ReadFromJsonAsync<PropertyReferenceResponse>(cancellationToken);
            return envelope?.Code == 20000 ? envelope.Data : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException
                                          && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Property references could not be checked; deletion is blocked.");
            return null;
        }
    }
}
