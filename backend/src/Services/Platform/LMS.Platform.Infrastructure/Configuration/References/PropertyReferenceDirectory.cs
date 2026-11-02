using LMS.BuildingBlocks.Core.References;
using LMS.Platform.Application.Configuration.Ports;
using LMS.Platform.Infrastructure.Authentication;
using LMS.Platform.Infrastructure.Quotas;
using Microsoft.Extensions.Options;

namespace LMS.Platform.Infrastructure.Configuration.References;

internal sealed class PropertyReferenceDirectory(
    IPropertyReferenceReader localReferences,
    PropertyReferenceClient remoteReferences,
    IOptions<BookingClientOptions> booking,
    IOptions<DispatchClientOptions> dispatch,
    IOptions<ReportingClientOptions> reporting) : IPropertyReferenceDirectory
{
    public async Task<bool?> HasReferencesAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (await localReferences.HasReferencesAsync(propertyId, cancellationToken)) return true;
        string[] addresses = [dispatch.Value.BaseAddress, booking.Value.BaseAddress, reporting.Value.BaseAddress];
        foreach (var address in addresses)
        {
            var referenced = await remoteReferences.HasReferencesAsync(address, propertyId, cancellationToken);
            if (referenced != false) return referenced;
        }
        return false;
    }
}
