using LMS.BuildingBlocks.Core.Authorization;

namespace LMS.Platform.Application.Authentication.Models;

// 登录或刷新成功后交给调用方的一组令牌。RefreshToken 是只此一次返回的明文，服务端仅保存其摘要。
public sealed record AppSession(
    string AccessToken,
    int ExpiresInSeconds,
    string RefreshToken,
    int RefreshExpiresInSeconds,
    PrincipalType PrincipalType);
