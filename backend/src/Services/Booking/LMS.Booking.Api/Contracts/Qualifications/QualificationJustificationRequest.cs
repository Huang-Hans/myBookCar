namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 升级资格例外或批准资格例外的请求体；不接收积分或外部核验字段。
/// </summary>
/// <param name="Justification">升级或批准例外的说明，必填且去除首尾空白后不能为空。</param>
public sealed record QualificationJustificationRequest(string? Justification);
