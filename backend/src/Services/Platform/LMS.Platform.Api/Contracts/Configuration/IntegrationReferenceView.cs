namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 外部集成参考视图，登记外部系统集成的配置参考，不代表实时连通性。
/// </summary>
/// <param name="IntegrationCode">集成代码，LOYALTY / PMS / SMS / EMAIL / MAPPING / GOOGLE_MAPS / AMAP。</param>
/// <param name="Name">集成显示名称。</param>
/// <param name="ConfiguredStatus">配置参考状态标签（原型为 Connected）；开放参考字符串，不表示已实时连通。</param>
/// <param name="Purpose">集成用途说明。</param>
/// <param name="ConfigurationOwnership">配置管理归属说明。</param>
/// <param name="EnvironmentLabel">环境显示标签。</param>
/// <param name="DataSource">数据来源说明。</param>
/// <param name="AccessPolicy">访问策略说明，仅作参考、不含凭据。</param>
public sealed record IntegrationReferenceView(
    string IntegrationCode,
    string Name,
    string ConfiguredStatus,
    string Purpose,
    string ConfigurationOwnership,
    string EnvironmentLabel,
    string DataSource,
    string AccessPolicy);
