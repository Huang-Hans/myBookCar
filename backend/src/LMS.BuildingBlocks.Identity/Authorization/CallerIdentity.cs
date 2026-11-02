namespace LMS.BuildingBlocks.Identity.Authorization;

// 已验证令牌中的稳定主体：发行方来源代码 + 主体标识。不包含任何请求头里的 userId/role。
// VerifiedEmail 只在发行方确认邮箱可信时填入，登录时用于首次关联和已关联用户的邮箱核对；否则为 null。
public sealed record CallerIdentity(string ProviderCode, string SubjectId, string? VerifiedEmail = null);
