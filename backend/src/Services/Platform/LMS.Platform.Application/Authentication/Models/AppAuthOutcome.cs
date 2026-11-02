namespace LMS.Platform.Application.Authentication.Models;

public enum AppAuthOutcome
{
    Succeeded,

    // 凭据无效、密码未设置、刷新凭据无效/过期/重用：对外统一为 40100，不区分原因。
    NotAuthenticated,

    // 同一登录标识连续失败过多，暂时拒绝。
    TooManyAttempts,

    // 签名密钥未配置，或必要的所属服务核对暂时不可用：不降级放行。
    SourceUnavailable
}
