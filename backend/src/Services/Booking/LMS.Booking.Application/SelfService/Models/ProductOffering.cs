using LMS.Booking.Application.SelfService.Options;

namespace LMS.Booking.Application.SelfService.Models;

// 一个服务产品对当前客户的可订结论。UnavailableReason 在不可订时给出：产品未配置的原因，或客户级原因（PROPERTY_UNASSIGNED 等）。
public sealed record ProductOffering(ServiceProductOption Product, bool IsBookable, string? UnavailableReason);
