namespace LMS.ApiGateway.Documentation.Sources;

// 一个下游服务的接口文档来源：Key 用于缓存与重命名前缀，GroupName 是聚合视图中的分组名。
internal sealed record DownstreamDocument(string Key, string Title, string GroupName, string Path, string Url);
