using LMS.Dispatch.Application.Vehicles.Models;

namespace LMS.Dispatch.Application.Vehicles.Ports;

public interface IBookingVehicleModelReader
{
    Task<IReadOnlyList<BookingVehicleModel>> ListBookingModelsAsync(string propertyId, CancellationToken cancellationToken);
}
