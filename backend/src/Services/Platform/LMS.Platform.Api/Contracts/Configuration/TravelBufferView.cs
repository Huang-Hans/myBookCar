namespace LMS.Platform.Api.Contracts.Configuration;

/// <summary>
/// 行程缓冲时间配置视图，按路线分类给出预留时间与导航来源。
/// </summary>
/// <param name="RouteCategoryCode">路线分类代码，使用上传稳定代码而非表格行号。</param>
/// <param name="RouteCategory">路线分类显示名称。</param>
/// <param name="BufferMinutes">缓冲时间分钟数（>=0）。</param>
/// <param name="NavigationProvider">导航提供方，GOOGLE_MAPS / AMAP。</param>
/// <param name="IsTrafficAwareEtaEnabled">是否已配置路况感知 ETA，不代表已实时调用成功。</param>
/// <param name="ApplicationRule">适用范围说明。</param>
public sealed record TravelBufferView(
    string RouteCategoryCode,
    string RouteCategory,
    int BufferMinutes,
    string NavigationProvider,
    bool IsTrafficAwareEtaEnabled,
    string ApplicationRule);
