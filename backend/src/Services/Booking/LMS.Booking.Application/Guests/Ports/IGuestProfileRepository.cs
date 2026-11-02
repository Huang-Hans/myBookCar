using LMS.Booking.Application.Abstractions;
using LMS.Booking.Domain.Guests;

namespace LMS.Booking.Application.Guests.Ports;

public interface IGuestProfileRepository
{
    Task<GuestProfile?> FindAsync(string guestId, ITransactionContext context, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(GuestProfile guest, ITransactionContext context, CancellationToken cancellationToken);
}
