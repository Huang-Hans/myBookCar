using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.DispatchOrders.Models;

public sealed record DispatchOrderCountQuery(PropertyScope Scope, DispatchOrderFilter Filter, DispatchOrderTripSets Sets);
