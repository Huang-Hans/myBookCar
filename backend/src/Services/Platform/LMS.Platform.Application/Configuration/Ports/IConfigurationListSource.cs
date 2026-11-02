using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Ports;

// 以列表形式存在的配置区域（每行有稳定代码）。实现负责从当前生效发布读取并映射为强类型条目。
public interface IConfigurationListSource<TItem>
{
    Task<AssetLookup<IReadOnlyList<TItem>>> LoadAsync(CancellationToken cancellationToken);
}
