namespace LMS.ApiGateway.Contracts.Authentication;

/// <summary>身份提供方签发的令牌；可选字段在没有时省略。</summary>
/// <param name="AccessToken">用于 Authorization 请求头的访问令牌。</param>
/// <param name="ExpiresIn">访问令牌有效期（秒）。</param>
/// <param name="TokenType">身份提供方返回的令牌类型。</param>
/// <param name="RefreshToken">刷新令牌；未签发时省略。</param>
/// <param name="RefreshExpiresIn">刷新令牌有效期（秒）；未提供时省略。</param>
/// <param name="Scope">已授予的权限范围；未提供时省略。</param>
/// <param name="SessionState">身份提供方的会话标识；未提供时省略。</param>
/// <param name="NotBeforePolicy">身份提供方的 not-before 策略；未提供时省略。</param>
public sealed record LoginResponse(
    string AccessToken,
    long ExpiresIn,
    string TokenType,
    string? RefreshToken,
    long? RefreshExpiresIn,
    string? Scope,
    string? SessionState,
    long? NotBeforePolicy);
