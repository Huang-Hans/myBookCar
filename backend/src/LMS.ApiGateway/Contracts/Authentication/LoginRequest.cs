using System.ComponentModel.DataAnnotations;

namespace LMS.ApiGateway.Contracts.Authentication;

/// <summary>转发给已配置身份提供方的登录凭据。</summary>
public sealed class LoginRequest
{
    /// <summary>身份提供方的用户名。</summary>
    [Required]
    public string Username { get; init; } = string.Empty;

    /// <summary>密码，按原样转发。</summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;
}
