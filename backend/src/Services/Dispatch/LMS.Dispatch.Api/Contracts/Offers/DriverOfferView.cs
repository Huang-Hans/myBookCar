namespace LMS.Dispatch.Api.Contracts.Offers;

/// <summary>
/// 派单邀请。倒计时以服务端 serverTime 与 expiresAt 为准，客户端本地计时器只用于展示。
/// </summary>
/// <param name="OfferId">邀请标识；每轮邀请唯一，不与行程标识混用。</param>
/// <param name="TripId">行程标识。</param>
/// <param name="TripReference">行程业务单号。</param>
/// <param name="Status">PENDING / ACCEPTING / ACCEPTED / DECLINED / EXPIRED / SUPERSEDED。ACCEPTING 表示已接受、等待权威指派确认，可重试同一邀请。</param>
/// <param name="ExpiresAt">邀请过期时间（UTC）。</param>
/// <param name="CreatedAt">邀请创建时间（UTC）。</param>
/// <param name="RespondedAt">司机响应或邀请关闭时间（UTC）；未响应时省略。</param>
/// <param name="ServerTime">本次响应的服务端时间（UTC），用于校准倒计时。</param>
/// <param name="Trip">行程摘要快照。</param>
/// <param name="Version">邀请版本号。</param>
public sealed record DriverOfferView(
    string OfferId,
    string TripId,
    string TripReference,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset ServerTime,
    OfferTripView Trip,
    long Version);
