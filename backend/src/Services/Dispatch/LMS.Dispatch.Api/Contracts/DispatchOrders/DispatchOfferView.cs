namespace LMS.Dispatch.Api.Contracts.DispatchOrders;

/// <summary>
/// 一轮派单邀请（客服视角）。倒计时以 serverTime 与 expiresAt 为准；界面计时结束后应重新查询服务端，而不是自行改写状态。
/// </summary>
/// <param name="OfferId">邀请标识。</param>
/// <param name="Round">第几轮邀请，从 1 起；早于轮次记录的存量邀请为 0。</param>
/// <param name="Status">PENDING / ACCEPTING / ACCEPTED / DECLINED / EXPIRED / SUPERSEDED。</param>
/// <param name="DriverId">被邀请的司机标识。</param>
/// <param name="DriverName">发送时的司机姓名；存量邀请省略。</param>
/// <param name="CreatedBy">发送邀请的经办人标识；存量邀请省略。</param>
/// <param name="CreatedAt">发送时间（UTC）。</param>
/// <param name="ExpiresAt">邀请过期时间（UTC）。</param>
/// <param name="RespondedAt">司机响应或邀请关闭时间（UTC）；仍未响应时省略。</param>
/// <param name="ServerTime">本次响应的服务端时间（UTC），用于校准倒计时。</param>
/// <param name="Version">邀请版本号。</param>
public sealed record DispatchOfferView(
    string OfferId,
    int Round,
    string Status,
    string DriverId,
    string? DriverName,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset ServerTime,
    long Version);
