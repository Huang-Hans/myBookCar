using System.ComponentModel.DataAnnotations;

namespace LMS.Platform.Api.Contracts.AppAuthentication;

/// <summary>此前登录或刷新返回的刷新令牌。</summary>
public sealed class AppRefreshRequest
{
    /// <summary>刷新令牌明文。每个刷新令牌只能使用一次，使用后返回新的刷新令牌；重复使用会撤销整条刷新链。</summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
