namespace LMS.Booking.Api.Contracts.Qualifications;

/// <summary>
/// 发起经理复审申请的请求体。申请人、角色和申请时间来自令牌与服务端时钟，不接受请求正文给出。
/// </summary>
/// <param name="GuestId">宾客标识。</param>
/// <param name="PropertyId">物业代码，必须在调用者授权范围内。</param>
/// <param name="Justification">申请理由，必填且去除首尾空白后不能为空。</param>
public sealed record CreateQualificationRequest(string? GuestId, string? PropertyId, string? Justification);
