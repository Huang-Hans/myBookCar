using System.ComponentModel.DataAnnotations;

namespace LMS.Platform.Api.Contracts.AppAuthentication;

/// <summary>司机或客户的手机号密码登录凭据。身份类型由登录入口固定，请求中不接受 role、principalType、driverId、guestId、identityProvider，也不再接受 loginId。</summary>
public sealed class AppLoginRequest
{
    /// <summary>手机号，例如 +85361234567 或 13800138001；空格、连字符和圆括号会被忽略。不带国家码时按位数推断（11 位中国手机号补 +86、8 位澳门号码补 +853），规范化后与所属表中的 phone 精确匹配。</summary>
    [Required]
    public string Phone { get; init; } = string.Empty;

    /// <summary>密码明文，仅用于本次校验，不会被保存或记录；不会被去除空白或规范化。尚未设置密码的账号不能登录。</summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; init; } = string.Empty;
}
