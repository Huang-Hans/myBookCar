using LMS.Dispatch.Application.Abstractions;
using LMS.Dispatch.Application.Offers.Models;

namespace LMS.Dispatch.Application.Offers.Commands;

// 司机响应邀请。DriverId 来自令牌绑定，不来自请求。
public sealed record RespondToOfferCommand(string OfferId, string DriverId, OfferDecision Decision, CommandContext Context);
