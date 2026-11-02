namespace LMS.Platform.Application.Configuration.Models;

// 路线分类缓冲。IsTrafficAwareEtaEnabled 只表示配置开启，不表示已实时调用成功。
public sealed record TravelBuffer(
    string RouteCategoryCode,
    string RouteCategory,
    int BufferMinutes,
    NavigationProvider NavigationProvider,
    bool IsTrafficAwareEtaEnabled,
    string ApplicationRule);
