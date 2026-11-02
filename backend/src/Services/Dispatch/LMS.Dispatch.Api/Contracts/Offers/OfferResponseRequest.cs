namespace LMS.Dispatch.Api.Contracts.Offers;

/// <summary>
/// 响应邀请的请求体。
/// </summary>
/// <param name="Decision">ACCEPT 或 DECLINE。</param>
public sealed record OfferResponseRequest(string? Decision);
