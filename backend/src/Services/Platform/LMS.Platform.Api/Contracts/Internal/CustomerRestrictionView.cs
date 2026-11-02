namespace LMS.Platform.Api.Contracts.Internal;

/// <summary>
/// 客户限制命中结果视图；没有命中时接口返回空数组。
/// </summary>
/// <param name="MemberId">命中的会员 ID。</param>
/// <param name="Reason">命中的限制原因。</param>
public sealed record CustomerRestrictionView(string MemberId, string Reason);
