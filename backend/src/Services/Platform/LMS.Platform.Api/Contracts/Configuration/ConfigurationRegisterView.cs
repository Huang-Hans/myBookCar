namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 配置登记视图，说明某个配置区域的管理机制、归属与入口。
/// </summary>
/// <param name="AreaCode">稳定的配置区域代码。</param>
/// <param name="AreaName">配置区域显示名称。</param>
/// <param name="Mechanism">管理机制，CONFIGURATION_UPLOAD / PORTAL_SCREEN。</param>
/// <param name="IsEditable">是否可通过 Portal 编辑。</param>
/// <param name="Owner">配置归属方标签。</param>
/// <param name="VersionLabel">版本显示标签，仅用于展示，不能冒充 Int64 版本号。</param>
/// <param name="SourceLabel">来源或页面标签。</param>
/// <param name="ModulePath">允许的前端站内路径，不是 API 地址或任意重定向。</param>
public sealed record ConfigurationRegisterView(
    string AreaCode,
    string AreaName,
    string Mechanism,
    bool IsEditable,
    string Owner,
    string VersionLabel,
    string SourceLabel,
    string ModulePath);
