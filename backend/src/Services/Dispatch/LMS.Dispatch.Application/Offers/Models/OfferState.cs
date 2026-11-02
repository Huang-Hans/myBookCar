using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Models;

// 某个行程最近一轮邀请的状态摘要，用于不加载整张邀请就能分类派单队列。
public sealed record OfferState(string TripId, DriverOfferStatus Status, DateTimeOffset ExpiresAt);
