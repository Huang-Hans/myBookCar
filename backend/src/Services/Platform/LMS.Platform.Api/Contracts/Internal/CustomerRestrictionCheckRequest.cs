namespace LMS.Platform.Api.Contracts.Internal;

/// <summary>
/// 服务间批量核对客户限制的请求体。
/// </summary>
/// <param name="MemberIds">待核对的会员 ID 列表，0 至 500 项，元素不得为空白。</param>
public sealed record CustomerRestrictionCheckRequest(List<string?>? MemberIds);
