using LMS.Dispatch.Domain.Offers;

namespace LMS.Dispatch.Application.Offers.Models;

// 响应邀请的结果。IsProcessing 为 true 表示已接受但 Booking 的权威指派尚未确认（跨服务处理中），可用同一邀请重试。
public sealed record OfferResponse(DriverOffer Offer, bool IsProcessing);
