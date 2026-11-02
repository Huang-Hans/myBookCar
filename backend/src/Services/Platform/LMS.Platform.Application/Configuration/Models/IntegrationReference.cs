namespace LMS.Platform.Application.Configuration.Models;

// 集成配置参考卡片。ConfiguredStatus 只是配置参考标签，不代表实时连通性；不含任何凭据。
public sealed record IntegrationReference(
    string IntegrationCode,
    string Name,
    string ConfiguredStatus,
    string Purpose,
    string ConfigurationOwnership,
    string EnvironmentLabel,
    string DataSource,
    string AccessPolicy,
    IntegrationGroup Group);
