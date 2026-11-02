namespace LMS.Dispatch.Api.Contracts.Internal;

/// <summary>
/// 服务间契约：司机当前是否仍可登录或刷新令牌（记录存在、已设置密码且为 ACTIVE）。
/// </summary>
/// <param name="IsEligible">是否可登录。</param>
public sealed record LoginEligibilityView(bool IsEligible);
