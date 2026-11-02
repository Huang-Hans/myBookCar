namespace LMS.Platform.Application.Configuration.Options;

// 配置正文读取设置（Q10）。Environment 是读取哪个环境的生效发布；
// 为空表示尚未指定环境，此时所有配置读取都按“来源不可用”处理，不回退到任何默认环境。
public sealed class ConfigurationAssetsOptions
{
    public const string SectionName = "ConfigurationAssets";

    public string Environment { get; init; } = string.Empty;
}
