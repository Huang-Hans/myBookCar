namespace LMS.Platform.Application.Configuration.Models;

// 配置正文的读取结果。IsAvailable=false 表示来源不可用（未配置环境、没有生效发布、该区域尚无正文或正文损坏），
// 与“来源可用但没有记录”的真实空列表严格区分。
public sealed record AssetLookup<T>(bool IsAvailable, T? Value)
{
    public static AssetLookup<T> Unavailable { get; } = new(false, default);

    public static AssetLookup<T> Available(T value) => new(true, value);
}
