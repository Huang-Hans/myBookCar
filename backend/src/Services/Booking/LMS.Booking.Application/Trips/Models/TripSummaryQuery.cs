using LMS.BuildingBlocks.Core.Scoping;

namespace LMS.Booking.Application.Trips.Models;

// 状态计数的共同筛选：与行程列表使用同一套范围、时间、搜索规则，但不含 status 本身（页签数字要在其余筛选下给出每个状态的数量）。
public sealed record TripSummaryQuery(
    PropertyScope Scope,
    string? PropertyId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? RedemptionState,
    string Search);
