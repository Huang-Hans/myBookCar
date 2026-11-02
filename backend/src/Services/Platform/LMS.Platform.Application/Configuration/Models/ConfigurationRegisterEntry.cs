namespace LMS.Platform.Application.Configuration.Models;

// 配置所有权台账的一行。VersionLabel 是显示标签，不能冒充发布序号（Q10）。
public sealed record ConfigurationRegisterEntry(
    string AreaCode,
    string AreaName,
    ConfigurationMechanism Mechanism,
    bool IsEditable,
    string Owner,
    string VersionLabel,
    string SourceLabel,
    string ModulePath);
