using Microsoft.Extensions.Configuration;

namespace LMS.AppHost.Extensions;

internal static class AppIdentityResourceExtensions
{
    // 司机/客户本地令牌的密钥来自 AppHost 的用户机密（LmsApp:PublicKeyPem / LmsApp:PrivateKeyPem），不写入仓库。
    // 未配置时不注入环境变量：服务以“本地令牌一律拒绝、登录返回 503”的方式启动，而不是带着占位密钥运行。
    public static IResourceBuilder<ProjectResource> WithAppTokenVerification(
        this IResourceBuilder<ProjectResource> resource,
        IConfiguration configuration) =>
        WithOptionalEnvironment(resource, "AppIdentity__PublicKeyPem", configuration["LmsApp:PublicKeyPem"]);

    // 只有签发方 Platform 持有私钥。
    public static IResourceBuilder<ProjectResource> WithAppTokenSigning(
        this IResourceBuilder<ProjectResource> resource,
        IConfiguration configuration) =>
        WithOptionalEnvironment(
            resource.WithAppTokenVerification(configuration), "AppSigning__PrivateKeyPem", configuration["LmsApp:PrivateKeyPem"]);

    private static IResourceBuilder<ProjectResource> WithOptionalEnvironment(
        IResourceBuilder<ProjectResource> resource,
        string name,
        string? value) =>
        string.IsNullOrWhiteSpace(value) ? resource : resource.WithEnvironment(name, value);
}
