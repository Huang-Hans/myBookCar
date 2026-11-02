using System.ComponentModel.DataAnnotations;

namespace LMS.ApiGateway.Contracts.Authentication;

/// <summary>此前由身份提供方签发的刷新令牌。</summary>
public sealed class RefreshTokenRequest
{
    /// <summary>登录或上一次刷新返回的刷新令牌，按原样转发。</summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
