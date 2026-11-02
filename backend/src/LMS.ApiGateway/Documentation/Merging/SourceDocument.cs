using System.Text.Json.Nodes;
using LMS.ApiGateway.Documentation.Sources;

namespace LMS.ApiGateway.Documentation.Merging;

// 参与合并的一份下游文档。Root 是可修改的副本，合并过程会重命名其中冲突的组件引用。
internal sealed record SourceDocument(DownstreamDocument Source, JsonObject Root);
