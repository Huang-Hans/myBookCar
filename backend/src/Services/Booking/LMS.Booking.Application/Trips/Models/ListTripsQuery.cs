using LMS.BuildingBlocks.Core.Querying;
using LMS.BuildingBlocks.Core.Scoping;
using LMS.Booking.Domain.Trips;

namespace LMS.Booking.Application.Trips.Models;

public sealed record ListTripsQuery(
    ListQuery List,
    PropertyScope Scope,
    string? PropertyId,
    TripStatus? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? RedemptionState,
    TripChannel? Channel = null);

// 取消前政策展示（HA-10）：政策引用来自 Platform 物业配置（CF01/CF02）。
public sealed record TripCancellationPolicy(string TripReference, string PropertyId, TripStatus Status, string? PolicyReference);
