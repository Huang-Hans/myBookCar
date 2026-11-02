namespace LMS.Booking.Api.Contracts.Internal;

/// <summary>
/// 服务间契约：客户当前是否仍可登录或刷新令牌（记录存在且已设置密码）。
/// </summary>
/// <param name="IsEligible">是否可登录。</param>
public sealed record LoginEligibilityView(bool IsEligible);
