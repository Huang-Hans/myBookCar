namespace LMS.Platform.Api.Contracts.Internal;

/// <summary>
/// 服务间查询通知收件人的请求体。
/// </summary>
/// <param name="EventType">事件类型，当前仅支持 BOOKING_CREATED。</param>
/// <param name="PropertyId">订单所属物业，必填且非空白。</param>
public sealed record NotificationRecipientsQueryRequest(string? EventType, string? PropertyId);
