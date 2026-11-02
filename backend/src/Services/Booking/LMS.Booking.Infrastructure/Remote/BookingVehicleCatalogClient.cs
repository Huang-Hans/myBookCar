using LMS.BuildingBlocks.Core.Authorization;
using LMS.BuildingBlocks.Core.Wire;
using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Trips.Models;
using LMS.Booking.Application.Trips.Ports;
using LMS.Booking.Domain.Trips;
using LMS.Booking.Infrastructure.ServiceIdentity;

namespace LMS.Booking.Infrastructure.Remote;

internal sealed class BookingVehicleCatalogClient(HttpClient client, IServiceTokenIssuer tokens) : IBookingVehicleCatalog
{
    public async Task<DirectoryResult<IReadOnlyList<BookingVehicleModel>>> ListAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (!tokens.IsAvailable)
            return DirectoryResult<IReadOnlyList<BookingVehicleModel>>.Failed(DirectoryOutcome.Unavailable);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"internal/v1/booking-vehicle-models?propertyId={Uri.EscapeDataString(propertyId)}");
        var response = await RemoteHttp.SendAsync(client, request, tokens.Issue(ServiceScopes.BookingVehicleCatalogRead), cancellationToken);
        return await RemoteHttp.ReadAsync<List<RemoteBookingVehicleModel>, IReadOnlyList<BookingVehicleModel>>(
            response, items => items.Select(ToModel).ToList(), cancellationToken);
    }

    private static BookingVehicleModel ToModel(RemoteBookingVehicleModel item) =>
        WireEnum.TryParse<VehicleClass>(item.VehicleClass, out var vehicleClass)
            ? new BookingVehicleModel(vehicleClass, item.Model, item.Seats)
            : throw new InvalidDataException("Unknown booking vehicle class.");
}
