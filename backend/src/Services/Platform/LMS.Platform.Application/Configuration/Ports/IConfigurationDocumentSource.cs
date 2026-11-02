using LMS.Platform.Application.Configuration.Models;

namespace LMS.Platform.Application.Configuration.Ports;

// 以单个说明对象形式存在的配置区域（规则说明、系统参数）。
public interface IConfigurationDocumentSource<TModel>
{
    Task<AssetLookup<TModel>> LoadAsync(CancellationToken cancellationToken);
}
