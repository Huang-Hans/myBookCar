using System.Text.Json;
using LMS.Booking.Application.Abstractions;

namespace LMS.Booking.Application.Qualifications.Commands;

// 升级或批准资格例外：只接收必填的说明，不接收积分或外部核验字段。
public sealed record QualificationExceptionCommand(
    string QualificationId,
    long ExpectedVersion,
    string Justification,
    string IdempotencyKey,
    CommandContext Context)
{
    public string ToCanonicalForm() => JsonSerializer.Serialize(new object?[] { QualificationId, ExpectedVersion, Justification });
}
