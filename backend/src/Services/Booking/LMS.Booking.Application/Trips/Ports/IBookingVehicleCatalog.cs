using LMS.Booking.Application.Directory;
using LMS.Booking.Application.Trips.Models;

namespace LMS.Booking.Application.Trips.Ports;

public interface IBookingVehicleCatalog
{
    Task<DirectoryResult<IReadOnlyList<BookingVehicleModel>>> ListAsync(string propertyId, CancellationToken cancellationToken);
}
