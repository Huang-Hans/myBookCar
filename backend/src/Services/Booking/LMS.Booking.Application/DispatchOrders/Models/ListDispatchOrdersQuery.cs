using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.DispatchOrders.Models;

public sealed record ListDispatchOrdersQuery(ListQuery List, PropertyScope Scope, DispatchOrderFilter Filter);
