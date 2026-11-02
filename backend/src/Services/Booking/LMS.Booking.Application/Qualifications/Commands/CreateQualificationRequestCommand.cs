using System.Text.Json;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Qualifications.Commands;

// 显式发起经理复审申请：宾客、物业与申请理由；申请人和时间来自服务端身份与时钟。
public sealed record CreateQualificationRequestCommand(
    string GuestId,
    string PropertyId,
    string Justification,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new object?[] { GuestId, PropertyId, Justification });
}
