namespace LMS.Platform.Application.Configuration.Catalogs;

// 在上传配置字典中查找一个代码的结果。SourceUnavailable 表示字典本身不可用，不能据此判定代码不存在。
public enum CatalogLookup
{
    Found,
    NotFound,
    SourceUnavailable
}
