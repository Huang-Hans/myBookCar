namespace LMS.Platform.Api.Contracts.AppAuthentication;

/// <summary>LMS 本地身份服务签发的令牌。</summary>
/// <param name="AccessToken">用于 Authorization 请求头的访问令牌（JWT），默认 15 分钟有效；已签发的访问令牌到期前不会被即时撤销。</param>
/// <param name="TokenType">固定为 Bearer。</param>
/// <param name="ExpiresIn">访问令牌有效期（秒）。</param>
/// <param name="RefreshToken">不透明刷新令牌，只用于对应入口的 token-refreshes 与 logouts。</param>
/// <param name="RefreshExpiresIn">刷新令牌剩余有效期（秒）；自登录起 7 天绝对有效，轮换不会延长。</param>
/// <param name="PrincipalType">当前登录身份：DRIVER 或 CUSTOMER，由登录入口固定。</param>
public sealed record AppSessionView(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    int RefreshExpiresIn,
    string PrincipalType);
