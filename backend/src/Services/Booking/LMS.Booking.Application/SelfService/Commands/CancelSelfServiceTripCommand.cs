using System.Text.Json;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.SelfService.Commands;

// 客户取消本人订单。原因码不由请求给出：由部署配置的自助取消原因映射（Q06）。ExpectedVersion 为 0 表示不比较版本。
public sealed record CancelSelfServiceTripCommand(
    string TripId,
    string GuestId,
    string? Comments,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new object?[] { TripId, GuestId, Comments });
}
