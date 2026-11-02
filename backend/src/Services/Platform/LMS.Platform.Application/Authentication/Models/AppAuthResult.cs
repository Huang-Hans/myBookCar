namespace LMS.Platform.Application.Authentication.Models;

public sealed record AppAuthResult(AppAuthOutcome Outcome, AppSession? Session)
{
    public static AppAuthResult Succeeded(AppSession session) => new(AppAuthOutcome.Succeeded, session);

    // 退出成功：没有会话可返回。
    public static AppAuthResult LoggedOut() => new(AppAuthOutcome.Succeeded, null);

    public static AppAuthResult Failed(AppAuthOutcome outcome) => new(outcome, null);
}
