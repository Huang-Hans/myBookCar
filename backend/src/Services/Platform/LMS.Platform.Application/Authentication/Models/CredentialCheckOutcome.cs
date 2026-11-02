namespace LMS.Platform.Application.Authentication.Models;

public enum CredentialCheckOutcome
{
    Verified,
    Rejected,

    // 所属服务无法给出真实结果（网络、配置或令牌问题）。
    Unavailable
}
