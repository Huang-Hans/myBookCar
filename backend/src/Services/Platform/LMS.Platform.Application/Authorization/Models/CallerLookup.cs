namespace LMS.Platform.Application.Authorization.Models;

// 登录关联所需的已验证令牌信息。VerifiedEmail 只在发行方确认邮箱可信时存在，用于首次登录关联。
public sealed record CallerLookup(string IdentityProvider, string SubjectId, string? VerifiedEmail, string TraceId);
