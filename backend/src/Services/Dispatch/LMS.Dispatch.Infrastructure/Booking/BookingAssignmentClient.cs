using System.Net.Http.Json;
using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Directory;

namespace LMS.Dispatch.Infrastructure.Booking;

// 通过 Booking 的服务间端点读取待分配行程、请求条件指派、核对司机的有效行程。转发的是终端用户自己的令牌，
// Booking 因此重新检查该用户的权限与本人身份；Dispatch 不持有额外的服务凭据，也不直接访问 Booking 的数据库。
internal sealed class BookingAssignmentClient(HttpClient client, ICallerToken callerToken) : IBookingAssignments
{
    public Task<BookingResult<AssignableTrip>> GetAssignableTripAsync(string tripId, CancellationToken cancellationToken) =>
        BookingHttp.SendAsync<AssignableTripPayload, AssignableTrip>(
            client,
            callerToken,
            new HttpRequestMessage(HttpMethod.Get, $"internal/v1/trips/{Uri.EscapeDataString(tripId)}/assignment-candidate"),
            payload => new AssignableTrip(
                payload.TripId, payload.TripReference, payload.PropertyId, payload.IsAssignable,
                payload.PickupAt, payload.PickupName, payload.DropoffName, payload.Passengers, payload.VehicleClass),
            cancellationToken);

    public Task<BookingResult<ActiveTripReference>> AssignDriverAsync(AssignDriverRequest request, CancellationToken cancellationToken)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, $"internal/v1/trips/{Uri.EscapeDataString(request.TripId)}/driver-assignments")
        {
            Content = JsonContent.Create(new
            {
                offerId = request.OfferId,
                driverId = request.DriverId,
                driverName = request.DriverName,
                vehicleLabel = request.VehicleLabel,
                propertyId = request.PropertyId
            })
        };

        return SendReferenceAsync(message, cancellationToken);
    }

    public Task<BookingResult<ActiveTripReference>> GetActiveTripAsync(CancellationToken cancellationToken) =>
        SendReferenceAsync(new HttpRequestMessage(HttpMethod.Get, "internal/v1/driver-active-trip"), cancellationToken);

    public Task<BookingResult<ActiveTripReference>> GetDriverTripAsync(string tripId, CancellationToken cancellationToken) =>
        SendReferenceAsync(new HttpRequestMessage(HttpMethod.Get, $"internal/v1/driver-trips/{Uri.EscapeDataString(tripId)}"), cancellationToken);

    private Task<BookingResult<ActiveTripReference>> SendReferenceAsync(HttpRequestMessage message, CancellationToken cancellationToken) =>
        BookingHttp.SendAsync<TripReferencePayload, ActiveTripReference>(
            client, callerToken, message, payload => new ActiveTripReference(payload.TripId, payload.TripReference), cancellationToken);
}
