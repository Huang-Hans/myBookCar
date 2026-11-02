namespace LMS.Platform.Api.Contracts.Internal;

/// <summary>
/// 一位收件人。
/// </summary>
/// <param name="UserId">Platform 本地用户标识（不是 IdP 的 sub）。</param>
public sealed record NotificationRecipientView(string UserId);
