using LMS.Dispatch.Domain.Orders;

namespace LMS.Dispatch.Application.Directory;

public sealed record BookingOrderCountQuery(
    string Search,
    string? PropertyId,
    OrderChannel? Channel,
    DateTimeOffset? From,
    DateTimeOffset? To,
    BookingOrderTripSets Sets);
