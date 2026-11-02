namespace LMS.Platform.Api.Contracts.Locations;

/// <summary>
/// 地点展示顺序整体替换请求体；目标区域取自请求路径，提交该区域内完整的地点顺序。
/// </summary>
/// <param name="Items">该区域的完整地点顺序，必须覆盖区域内全部地点且每项恰好出现一次；排序、搜索、分页都不得缩减该集合。</param>
public sealed record LocationOrderingRequest(List<LocationOrderItemRequest?>? Items);
