using System.ComponentModel.DataAnnotations;

namespace LMS.Platform.Api.Contracts.AppAuthentication;

/// <summary>要退出的登录会话，由其当前刷新令牌标识。</summary>
public sealed class AppLogoutRequest
{
    /// <summary>登录或上一次刷新返回的刷新令牌。退出会撤销它所在的整条刷新链；重复退出同样成功。</summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
